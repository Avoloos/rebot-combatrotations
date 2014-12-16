using System;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;
using ReBot.API;
using Geometry;

namespace Avoloos
{
    abstract public class CombatRotation : ReBot.API.CombatRotation
    {
        /// <summary>
        /// Should the OOC-Rotation be disabled for the Fishingbot?
        /// </summary>
        [JsonProperty("General: Disable OutOfCombat for FishBot")]
        public bool DisableOutOfCombatFishbot = true;

        /// <summary>
        /// Defines the factor of HP a unit has to have to be counted as a boss.
        /// </summary>
        [JsonProperty("Boss Setting: Percentual factor of a Targets MaxHP in relation to Players MaxHP to be valued as Bossencounter")]
        public int BossHealthPercentage = 500;

        /// <summary>
        /// Defines the +Level a Unit should have to be counted as a boss.
        /// </summary>
        [JsonProperty("Boss Setting: +Level a Target has to have to be valued as Boss encounter")]
        public int BossLevelIncrease = 5;

        /// <summary>
        /// Dictionary with all AoE effect ranges
        /// </summary>
        protected Dictionary<string, float> AoESpellRadius;

        /// <summary>
        /// Gets the spell power.
        /// </summary>
        /// <value>The spell power.</value>
        public int SpellPower {
            get {
                return API.ExecuteLua<int>("return GetSpellBonusDamage(7)");
            }
        }

        /// <summary>
        /// Checks if the given unit may be a boss unit.
        /// </summary>
        /// <returns><c>true</c>, if unit is (maybe) a boss, <c>false</c> otherwise.</returns>
        /// <param name="o">The Unit we want to check</param>
        public bool IsBoss(UnitObject o)
        {
            return ( o.MaxHealth >= Me.MaxHealth * ( BossHealthPercentage / 100f ) ) || o.Level >= Me.Level + BossLevelIncrease;
        }

        /// <summary>
        /// Determines whether the player has hand of guldan glyphed.
        /// </summary>
        /// <returns><c>true</c> if this hand of guldan is glyph; otherwise, <c>false</c>.</returns>
        /// <param name="spellId">Spell id.</param>
        public bool IsGlyphed(int spellId)
        {
            return API.LuaIf("for i = 1, NUM_GLYPH_SLOTS do local _,_,_,glyphSpellID,_ = GetGlyphSocketInfo(i); if(glyphSpellID == " + spellId + ") then return true end end return false");
        }

        /// <summary>
        /// Counts the enemies in players range.
        /// </summary>
        /// <returns>The enemies in players range.</returns>
        /// <param name="rangeSq">Squared Range.</param>
        public int CountEnemiesInPlayersRangeSquared(float rangeSq)
        {
            return Adds.Concat(new[] { Target }).Count(u => u.DistanceSquared <= rangeSq);
        }

        /// <summary>
        /// Returns the AoE Range of a spell
        /// </summary>
        /// <returns>The AoE range.</returns>
        /// <param name="spellName">Spell name.</param>
        public virtual float SpellAoERange(string spellName)
        {
            return -1;
        }

        /// <summary>
        /// Should return true if the spell is a known cast on terrain spell.
        /// </summary>
        /// <returns><c>true</c>, if spell has to be cast on terrain, <c>false</c> otherwise.</returns>
        /// <param name="spellName">Spell name.</param>
        public virtual bool SpellIsCastOnTerrain(string spellName)
        {
            return false;
        }

        /// <summary>
        /// Casts the given spell on the best target.
        /// If none is found it will always fallback to Target.
        /// </summary>
        /// <returns><c>true</c>, if spell on best target was cast, <c>false</c> otherwise.</returns>
        /// <param name="spellName">Spell name.</param>
        /// <param name="castWhen">onlyCastWhen condition for Cast()</param>
        /// <param name="bestTargetCondition">Condition to limit the UnitObjects for a bestTarget</param>
        /// <param name="preventTime">Milliseconds in which the spell won't be cast again</param>
        /// <param name="targetOverride">Spell will be cast on this target</param>
        public bool CastSpellOnBestAoETarget(string spellName, Func<UnitObject, bool> castWhen = null, Func<UnitObject, bool> bestTargetCondition = null, int preventTime = 0, UnitObject targetOverride = null)
        {
            if (castWhen == null)
                castWhen = ( _ => true );

            if (bestTargetCondition == null)
                bestTargetCondition = ( _ => true );

            var aoeRange = SpellAoERange(spellName);
            var bestTarget = targetOverride ?? Adds
                .Where(u => u.IsInCombatRangeAndLoS && u.DistanceSquared <= SpellMaxRangeSq(spellName) && bestTargetCondition(u))
                .OrderByDescending(u => Adds.Count(o => Vector3.DistanceSquared(u.Position, o.Position) <= aoeRange)).FirstOrDefault() ?? Target;

            if (preventTime == 0) {
                return SpellIsCastOnTerrain(spellName) ? CastOnTerrain(
                    spellName,
                    bestTarget.Position,
                    () => castWhen(bestTarget)
                ) : Cast(
                    spellName,
                    bestTarget, 
                    () => castWhen(bestTarget)
                );
            }

            return SpellIsCastOnTerrain(spellName) ? CastOnTerrainPreventDouble(
                spellName,
                bestTarget.Position,
                () => castWhen(bestTarget),
                preventTime
            ) : CastPreventDouble(
                spellName,
                () => castWhen(bestTarget),
                bestTarget, 
                preventTime
            );
        }

        /// <summary>
        /// Casts the spell on adds.
        /// </summary>
        /// <returns><c>true</c>, if spell on adds was cast, <c>false</c> otherwise.</returns>
        /// <param name="spellName">Spell name.</param>
        public bool CastSpellOnAdds(string spellName)
        {
            return CastSpellOnAdds(spellName, null);
        }

        /// <summary>
        /// Cast the spell on adds.
        /// </summary>
        /// <returns><c>true</c>, if spell on adds was cast, <c>false</c> otherwise.</returns>
        /// <param name="spellName">Spell name.</param>
        /// <param name="castCondition">Condition which gets a UnitObject to decide if the spell may get cast on it.</param>
        public bool CastSpellOnAdds(string spellName, Func<UnitObject, bool> castCondition)
        {
            castCondition = castCondition ?? ( add => true );

            foreach (var add in Adds) {
                if (Cast(
                        spellName,
                        () => castCondition(add),
                        add
                    ))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Casts the spell on adds. Will prevent multiple casts.
        /// </summary>
        /// <returns><c>true</c>, if spell prevent double on adds was cast, <c>false</c> otherwise.</returns>
        /// <param name="spellName">Spell name to cast.</param>
        /// <param name="castCondition">Condition which gets a UnitObject to decide if the spell may get cast on it.</param>
        public bool CastSpellPreventDoubleOnAdds(string spellName, Func<UnitObject, bool> castCondition)
        {
            castCondition = castCondition ?? ( add => true );

            foreach (var add in Adds) {
                if (castCondition != null && CastPreventDouble(
                        spellName,
                        () => castCondition(add),
                        add
                    ))
                    return true;
            }
            return false;
        }
    }
}

