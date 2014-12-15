using System.Linq;
using Newtonsoft.Json;
using ReBot.API;
using Avoloos.Warlock;

namespace Avoloos
{
    namespace Warlock
    {
        namespace IcyVeins
        {
            /// <summary>
            /// Affliction profile, based of IcyVeins for Patch 6.0.3
            /// </summary>
            [Rotation(
                "Warlock Affliction - Icy Veins Profile",
                "Avoloos",
                "Version: " + RotationVersion.WarlockIcyVeinsAffliction,
                WoWClass.Warlock,
                Specialization.WarlockAffliction,
                40
            )]
            public class Affliction : BaseRotation
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="Avoloos.Warlock.IcyVeins.Affliction"/> class.
                /// </summary>
                public Affliction()
                {
                    GroupBuffs = new[] {
                        "Dark Intent",
                        ( CurrentBotName == "PvP" ? "Create Soulwell" : null )
                    };
                    PullSpells = new[] {
                        "Agony",
                        "Corruption",
                        "Drain Soul"
                    };

                    Info("Warlock Affliction - Version " + RotationVersion.WarlockIcyVeinsAffliction + " by Avoloos.");
                }

                /// <summary>
                /// Does the multitarget rotation.
                /// </summary>
                /// <returns><c>true</c>, if a spell was cast, <c>false</c> otherwise.</returns>
                /// <param name="mobsInFrontOfMe">Mobs in front of me.</param>
                bool DoMultitargetRotation(int mobsInFrontOfMe)
                {
                    if (
                        mobsInFrontOfMe >= 3// Got a Group
                        && HasFelguard()// and Has a Felguard
                        && Cast("Command Demon") && HasGlobalCooldown())
                        return true;

                    /*
                     * Against 5 or more enemies, you will need to start using Soulburn with Seed of Corruption
                     */
                    if (mobsInFrontOfMe >= 5) {
                        CastSelf(
                            "Mannoroth's Fury",
                            () => HasSpell("Mannoroth's Fury") && !Me.HasAura("Mannoroth's Fury")
                        );
                        CastSelf("Soulburn", () => !Me.HasAura("Soulburn"));
                    }

                    if (Me.HasAura("Soulburn")) {
                        if (CastSpellOnBestAoETarget(
                                "Seed of Corruption",
                                add => !add.HasAura("Seed of Corruption"),
                                add => !add.HasAura("Seed of Corruption")
                            ))
                            return true;
                    }

                    /*
                     * Against 2 enemies, use your normal rotation on one of them and keep your DoTs up on the other.
                     * Against 3 or 4 enemies, keep your DoTs up and cast Drain Soul Icon Drain Soul. 
                     * Against 5 or more enemies, While Seed of Corruption is ticking, you should maintain your DoTs on as many targets as possible.
                     */
                    foreach (var add1 in Adds.Where(x => x.IsInCombatRangeAndLoS)) {
                        if (DoDotting(add1))
                            return true;
                    }

                    if (mobsInFrontOfMe >= 3) {
                        /*
                         * Against 3 or 4 enemies, keep your DoTs up and cast Drain Soul Icon Drain Soul. 
                         */
                        if (Cast("Drain Soul"))
                            return true;
                    }

                    return false;
                }

                /// <inheritdoc/>
                public override void Combat()
                {
                    if (Me.IsCasting && Me.CastingSpellID == (int) WarlockSpellIds.CATACLYSM)
                        return;

                    if (DoSharedRotation())
                        return;

                    //Adds
                    if (Adds.Count > 0) {
                        if (CastOnTerrain(
                                "Shadowfury",
                                Target.Position,
                                () => Adds.Count(x => x.DistanceSquaredTo(Target) <= 12 * 12) > 2
                            ))
                            return;
                        if (DoMultitargetRotation(Adds.Count + 1))
                            return;
                    }

                    // Single DPS
                    if (DoDotting(Target))
                        return;
                    if (CastPreventDouble(
                            "Haunt", 
                            () => 
                            ( !Target.HasAura("Haunt") || Me.GetPower(WoWPowerType.WarlockSoulShards) >= 4 )
                            && (
                                // TODO: Trinket Procc
                                Target.HealthFraction <= 0.25f// the boss is reaching death
                                || Me.GetPower(WoWPowerType.WarlockSoulShards) > 3// We capped it (sadly we don't get it when we reached half a shard) (and yes I know whis will get this equation to true as we check against >= 4 above!)
                                || Me.HasAura("Dark Soul: Misery")
                            )
                        ))
                        return;

                    // TODO: MultiDPS with Haunt, maybe?

                    // Okay.. now souldrain :D
                    if (Cast("Drain Soul"))
                        return;
                }

                /// <summary>
                /// Does the dotting.
                /// </summary>
                /// <returns><c>true</c>, if a spell was cast, <c>false</c> otherwise.</returns>
                /// <param name="u">The Unit to apply a dot to</param>
                bool DoDotting(UnitObject u)
                {
                    // Lets see what the community suggests we will use it on CD for now
                    if (HasSpell("Cataclysm")) {
                        if (CastSpellOnBestAoETarget("Cataclysm"))
                            return true;
                    }
                    if (Cast(
                            "Agony",
                            () => u.HpGreaterThanOrElite(0.3) && ( !u.HasAura("Agony") || u.AuraTimeRemaining("Agony") <= 7f ),
                            u
                        ))
                        return true;
                    if (Cast(
                            "Corruption",
                            () => u.HpGreaterThanOrElite(0.15) && ( !u.HasAura("Corruption") || u.AuraTimeRemaining("Corruption") <= 5f ),
                            u
                        ))
                        return true;
                    if (CastPreventDouble(
                            "Unstable Affliction",
                            () => u.HpGreaterThanOrElite(0.2) && ( !u.HasAura("Unstable Affliction") || u.AuraTimeRemaining("Unstable Affliction") <= 5f ),
                            u
                        ))
                        return true;
                    return false;
                }
            }
        }
    }
}