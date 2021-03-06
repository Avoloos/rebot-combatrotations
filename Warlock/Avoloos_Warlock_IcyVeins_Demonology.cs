using System;
using ReBot.API;
using Avoloos.Warlock;
using System.Linq;
using Newtonsoft.Json;

namespace Avoloos
{
    namespace Warlock
    {
        namespace IcyVeins
        {
            /// <summary>
            /// Demonology profile, based of IcyVeins for Patch 6.0.3
            /// </summary>
            [Rotation(
                "Warlock Demonology - Icy Veins Profile",
                "Avoloos",
                "Version: " + RotationVersion.WarlockIcyVeinsDemonology,
                WoWClass.Warlock,
                Specialization.WarlockDemonology,
                40
            )]
            public sealed class Demonology : BaseRotation
            {

                /// <summary>
                /// Should the bot use Terrorguard/Infernal
                /// </summary>
                [JsonProperty("DPS: Use Hellfire (disable for leveling!)")]
                public bool UseHellfire = false;

                /// <summary>
                /// Should the bot use Terrorguard/Infernal
                /// </summary>
                [JsonProperty("DPS: Minimal Health to do Hellfire in %")]
                public int HellfireHealthPercentage = 50;

                //[JsonProperty("DPS: Move near target for Hellfire (not used atm.)")]
                //public bool DoMoveHellfireImmolation = true;

                /// <summary>
                /// The hand of guldan spell lock.
                /// If this value is true Hand of Guldan will be cast, else it will not.
                /// </summary>
                bool UseHandOfGuldan = false;

                /// <summary>
                /// The minimum molten stacks for soulfire to be cast.
                /// </summary>
                int MinMoltenStacksForSoulfire = 2;

                /// <summary>
                /// Should he use Soulfire while he is morphed? 
                /// Espcially needed for Demonbolt usage.
                /// </summary>
                bool UseSoulfireWhileMorphed = true;

                /// <summary>
                /// Defines if the warlock is in the metamorphed state
                /// </summary>
                /// <value><c>true</c> if the warlokc is in metamorphosis; otherwise, <c>false</c>.</value>
                bool HasMetamorphosis {
                    get {
                        return Me.HasAura("Metamorphosis");
                    }
                }

                /// <summary>
                /// Initializes a new instance of the <see cref="Avoloos.Warlock.IcyVeins.Demonology"/> class.
                /// </summary>
                public Demonology()
                {
                    GroupBuffs = new[] {
                        "Dark Intent",
                        ( CurrentBotName == "PvP" ? "Create Soulwell" : null )
                    };
                    PullSpells = new[] {
                        "Corruption",
                        "Shadow Bolt"
                    };

                    Info("Warlock Demonology - Version " + RotationVersion.WarlockIcyVeinsDemonology + " by Avoloos.");
                }

                /// <summary>
                /// Does the multi target rotation.
                /// </summary>
                /// <returns><c>true</c>, if a spell was cast, <c>false</c> otherwise.</returns>
                /// <param name="mobsInFrontOfMe">Mobs in front of me.</param>
                bool DoMultiTargetRotation(int mobsInFrontOfMe)
                {
                    bool doSoulFire = true;
                    bool doHellfire = false;
                    bool doImmolationAura = false;
                    bool doCataclysm = false;

                    //bool doChaosWave = false; // TODO: Support it for easy groups of enemies.
                    bool dotAllTargets = false;

                    if (mobsInFrontOfMe >= 6) {
                        // Skip Soul Fire
                        doSoulFire = false;
                    } else if (mobsInFrontOfMe >= 4) {
                        // TODO: Support Mannoroth's Fury
                        doHellfire = !HasMetamorphosis && UseHellfire && CountEnemiesInPlayersRangeSquared(SpellAoERange("Hellfire")) >= 4;
                        MinMoltenStacksForSoulfire = 10;
                    } else if (mobsInFrontOfMe >= 3) {
                        doImmolationAura = HasMetamorphosis && CountEnemiesInPlayersRangeSquared(SpellAoERange("Immolation Aura")) >= 3;
                        doCataclysm = HasSpell("Cataclysm") && HasMetamorphosis && Target.IsElite();
                    } else {
                        dotAllTargets = true;
                    }

                    if (doCataclysm) {
                        // Apply Doom to all adds through Cataclysm
                        CastSpellOnBestAoETarget("Cataclysm");
                    }

                    if (dotAllTargets) { // Do all the Adds dotting.
                        if (HasMetamorphosis) {
                            if (CastSpellOnAdds(
                                    "Doom",
                                    add => ( add.HpGreaterThanOrElite(0.15) && ( !add.HasAura("Doom") || add.AuraTimeRemaining("Doom") <= 18f ) )
                                ))
                                return true;
 
                        } else {
                            if (CastSpellOnAdds(
                                    "Corruption",
                                    add => ( add.HpGreaterThanOrElite(0.15) && ( !add.HasAura("Corruption") || add.AuraTimeRemaining("Corruption") <= 6f ) )
                                ))
                                return true;
                        }
                    }

                    if (UseSoulfireWhileMorphed) {
                        if (Cast(
                                "Soul Fire",
                                () => doSoulFire
                                && ( Target.HealthFraction <= 0.25 )
                                || ( Me.HasAura("Molten Core", true, MinMoltenStacksForSoulfire) )
                            ))
                            return true;
                    } else {
                        if (Cast("Demonbolt", () => !HasAura("Demonbolt", true, 4)))
                            return true;
                    }

                    // TODO: find a way to get close to the enemies (leap there?)
                    if (doHellfire || doImmolationAura) {
                        CastSelf(
                            "Mannoroth's Fury",
                            () => HasSpell("Mannoroth's Fury") && !Me.HasAura("Mannoroth's Fury")
                        );
                        if (CastVariant(
                                "Hellfire",
                                "Immolation Aura",
                                Me,
                                null,
                                u => Me.HealthFraction > ( HellfireHealthPercentage / 100f )
                            ))
                            return true;
                    }

                    // TODO: find a way to integrate Chaos Wave if targets are easy

                    // Lets stick to our singleRotaiton if something above does not procc
                    return false;
                }

                /// <summary>
                /// Does the metamorphosis.
                /// </summary>
                /// <returns><c>true</c>, if spell was cast, <c>false</c> otherwise.</returns>
                bool DoMetamorphosis()
                {
                    if (UseHandOfGuldan)
                        return false;

                    var currentFury = Me.GetPower(WoWPowerType.WarlockDemonicFury);

                    if (HasMetamorphosis) {
                        if (CastSelf(
                                "Metamorphosis",
                                () => ( currentFury < 750 && !Me.HasAura("Dark Soul: Knowledge") && Target.HasAura(
                                    "Doom",
                                    true
                                ) )
                            ))
                            return true;
                    } else {
                        // TODO: Tune the Dark Soul condition a bit...
                        if (CastSelf(
                                "Metamorphosis",
                                () =>
                                Me.InCombat
                                && (
                                    ( currentFury >= 850 )
                                    || ( currentFury >= 400 && Me.HasAura("Dark Soul: Knowledge") )
                                    || ( currentFury >= 200 && Target.HasAura("Corruption", true) && !Target.HasAura("Doom") )
                                )
                            ))
                            return true;
                    }

                    return false;
                }

                /// <summary>
                /// Reset some variables on special conditions (like Hand of Gul'dan usage and the adhering morph lock)
                /// </summary>
                void ResetRotationVariables()
                {
                    UseSoulfireWhileMorphed = !HasSpell("Demonbolt");
                    MinMoltenStacksForSoulfire = 5;

                    if (SpellCharges("Hand of Gul'dan") >= 2) {
                        UseHandOfGuldan = true;
                    }

                    if (SpellCharges("Hand of Gul'dan") == 0) {
                        UseHandOfGuldan = false;
                    }
                }

                /// <summary>
                /// Casts the Variant Spell, this is used for methamorphosis casting.
                /// </summary>
                /// <returns><c>true</c>, if variant was cast, <c>false</c> otherwise.</returns>
                /// <param name="spellNameNormal">Spell name normal.</param>
                /// <param name="spellNameMetamorphosed">Spell name metamorphosed.</param>
                /// <param name="target">Target.</param>
                /// <param name="unitCondition">Unit condition.</param>
                /// <param name="normalCondition">Normal condition.</param>
                /// <param name="metamorphedCondition">Metamorphed condition.</param>
                bool CastVariant(string spellNameNormal, string spellNameMetamorphosed, UnitObject target = null, Func<UnitObject, bool> unitCondition = null, Func<UnitObject, bool> normalCondition = null, Func<UnitObject, bool> metamorphedCondition = null)
                {
                    if (target == null)
                        target = Target;
                    if (unitCondition == null)
                        unitCondition = ( _ => true );
                    if (normalCondition == null)
                        normalCondition = ( _ => true );
                    if (metamorphedCondition == null)
                        metamorphedCondition = ( _ => true );
                
                    return HasMetamorphosis ? Cast(
                        spellNameMetamorphosed,
                        () => unitCondition(target) && metamorphedCondition(target),
                        target
                    ) : Cast(
                        spellNameNormal,
                        () => unitCondition(target) && normalCondition(target),
                        target
                    );
                }

                /// <summary>
                /// Casts the Variant Spell, this is used for methamorphosis casting.
                /// </summary>
                /// <returns><c>true</c>, if variant was cast, <c>false</c> otherwise.</returns>
                /// <param name="spellIdNormal">Spell identifier normal.</param>
                /// <param name="spellIdMetamorphosed">Spell identifier metamorphosed.</param>
                /// <param name="target">Target.</param>
                /// <param name="unitCondition">Unit condition.</param>
                /// <param name="normalCondition">Normal condition.</param>
                /// <param name="metamorphedCondition">Metamorphed condition.</param>
                bool CastVariant(DemonologySpellIds spellIdNormal, DemonologySpellIds spellIdMetamorphosed, UnitObject target = null, Func<UnitObject, bool> unitCondition = null, Func<UnitObject, bool> normalCondition = null, Func<UnitObject, bool> metamorphedCondition = null)
                {
                    if (target == null)
                        target = Target;
                    if (unitCondition == null)
                        unitCondition = ( _ => true );
                    if (normalCondition == null)
                        normalCondition = ( _ => true );
                    if (metamorphedCondition == null)
                        metamorphedCondition = ( _ => true );

                    return HasMetamorphosis ? Cast(
                        (int) spellIdMetamorphosed,
                        () => unitCondition(target) && metamorphedCondition(target),
                        target
                    ) : Cast(
                        (int) spellIdNormal,
                        () => unitCondition(target) && normalCondition(target),
                        target
                    );
                }

                /// <inheritdoc/>
                override public void Combat()
                {
                    if (Me.IsCasting && Me.CastingSpellID == (int) WarlockSpellIds.CATACLYSM)
                        return;

                    // reset some demonology vars (like hand of gul'dan lock)
                    ResetRotationVariables();

                    if (DoSharedRotation())
                        return;

                    if (DoMetamorphosis())
                        return;

                    // Always do Hand of Gul'dan id available and before tick ends
                    // highest prio!
                    if (CastSpellOnBestAoETarget(
                            "Hand of Gul'dan",
                            u => !HasMetamorphosis && UseHandOfGuldan && u.AuraTimeRemaining("Hand of Gul'dan") <= 3f,
                            null,
                            2000
                        ))
                        return;

                    // Icy Veins Rotation
                    if (Adds.Count > 0 && DoMultiTargetRotation(Adds.Count + 1))
                        return;

                    // Then we do the rest of our dots
                    if (CastSpellOnBestAoETarget(
                            "Cataclysm",
                            u => ( HasMetamorphosis && !u.HasAura("Doom", true) ) || ( !HasMetamorphosis && !u.HasAura(
                                "Corruption",
                                true
                            ) ),
                            u => ( HasMetamorphosis && !u.HasAura("Doom", true) ) || ( !HasMetamorphosis && !u.HasAura(
                                "Corruption",
                                true
                            ) )
                        ))
                        return;

                    if (CastVariant(
                            "Corruption",
                            "Doom",
                            Target,
                            u => u.HpGreaterThanOrElite(0.15),
                            u => !u.HasAura("Corruption", true) || u.AuraTimeRemaining("Corruption") <= 4f,
                            u => !u.HasAura("Doom", true) || u.AuraTimeRemaining("Doom") <= 18f
                        ))
                        return;

                    if (!UseSoulfireWhileMorphed) {
                        // If we don't use Soulfire while morphed we can use it as soon as we get a molten stack
                        // Because it seems we want to spend out demonic energy otherwise (e.g. Demonbolt)
                        if (Cast("Soul Fire", () => Me.HasAura("Molten Core")))
                            return;
                    } else {
                        if (Cast(
                                "Soul Fire", 
                                () => ( 
                                    Target.IsElite()
                                    && ( 
                                        ( Me.HasAura(
                                            "Molten Core", 
                                            true
                                        ) && Target.HealthFraction <= 0.25 )
                                        || Me.HasAura(
                                            "Molten Core",
                                            true,
                                            MinMoltenStacksForSoulfire
                                        )
                                    ) 
                                )
                                || ( !Target.IsElite() && Me.HasAura("Molten Core") )
                            ))
                            return;
                    }
                    // Fallback cast variant
                    if (CastVariant("Shadow Bolt", "Touch of Chaos"))
                        return;
                }
            }
        }
    }
}
