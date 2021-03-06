using System.Linq;
using ReBot.API;
using System;
using Avoloos.Warlock;
using Newtonsoft.Json;

namespace Avoloos
{
    namespace Warlock
    {
        namespace IcyVeins
        {
            /// <summary>
            /// Destruction profile, based of IcyVeins for Patch 6.0.3
            /// </summary>
            [Rotation(
                "Warlock Destruction - Icy Veins Profile",
                "Avoloos",
                "Version: " + RotationVersion.WarlockIcyVeinsDestruction,
                WoWClass.Warlock,
                Specialization.WarlockDestruction,
                40
            )]
            public class Destruction : BaseRotation
            {
                /// <summary>
                /// Health in % the target of havoc should have.
                /// </summary>
                [JsonProperty("DPS: Use Havoc on Mobs with HP in %")]
                public int HavocHealthPercentage = 40;

                /// <summary>
                /// Should havoc only be cast on focus / focus target if focus is friendly
                /// </summary>
                [JsonProperty("DPS: Use Havoc on your Focus (if friendly on its Target)")]
                public bool UseHavocOnFocus = true;

                /// <summary>
                /// The immolate lock.
                /// </summary>
                readonly Countdown ImmolateLock = new Countdown(new TimeSpan(0, 0, 2), true);

                /// <summary>
                /// Gets the shadow burn damage.
                /// </summary>
                /// <value>The shadow burn damage.</value>
                int ShadowBurnDamage {
                    get {
                        return (int) ( ( ( 315 / 100f ) * SpellPower ) * 1.24 );
                    }
                }

                /// <summary>
                /// Initializes a new instance of the <see cref="Avoloos.Warlock.IcyVeins.Destruction"/> class.
                /// </summary>
                public Destruction()
                {
                    GroupBuffs = new[] {
                        "Dark Intent",
                        ( CurrentBotName == "PvP" ? "Create Soulwell" : null )
                    };
                    PullSpells = new[] {
                        "Immolate",
                        "Conflagrate",
                        "Incinerate"
                    };

                    Info("Warlock Destruction - Version " + RotationVersion.WarlockIcyVeinsDestruction + " by Avoloos.");
                }

                /// <summary>
                /// Does the multitarget rotation.
                /// </summary>
                /// <returns><c>true</c>, if a spell was cast, <c>false</c> otherwise.</returns>
                /// <param name="mobsInFrontOfMe">Mobs in front of me.</param>
                bool DoMultitargetRotation(int mobsInFrontOfMe)
                {
                    int burningEmbers = Me.GetPower(WoWPowerType.WarlockDestructionBurningEmbers);
           
                    if (mobsInFrontOfMe >= 3)
                        CastSelf(
                            "Mannoroth's Fury",
                            () => HasSpell("Mannoroth's Fury") && !Me.HasAura("Mannoroth's Fury")
                        );

                    // Priority #1
                    if (CastSpellOnBestAoETarget(
                            "Rain of Fire",
                            u => !HasAura("Rain of Fire") && ( HasAura("Mannoroth's Fury") || mobsInFrontOfMe >= 5 )
                        ))
                        return true;

                    // Priority #2
                    if (
                        SpellCooldown("Havoc") <= 0.01 && burningEmbers >= 1 && mobsInFrontOfMe < 12) {
                        // Dont waste Havoc apply it to one of the mid-enemies (high max health, low current health)
                        var havocAdd = Me.Focus;

                        if (!UseHavocOnFocus)
                            havocAdd = Adds
                                .OrderByDescending(x => x.Health)
                                .FirstOrDefault(x => x.HealthFraction <= HavocHealthPercentage / 100f && x.IsInLoS && x.DistanceSquared <= SpellMaxRangeSq("Havoc")) ?? Adds.FirstOrDefault();

                        if (havocAdd != null && havocAdd.IsFriendly)
                            havocAdd = havocAdd.Target;

                        if (havocAdd != null && Cast("Havoc", havocAdd))
                            return true;
                    }

                    // cast Chaosbolt or shadowburn on target as soon as possible and if feasible
                    if (Adds.Count(x => x.HasAura("Havoc", true)) > 0 || burningEmbers >= 4) {
                        var shadowBurnTarget = Adds
                            .Where(x => x.HealthFraction <= 0.2 && !x.HasAura("Havoc") && x.IsInLoS && x.DistanceSquared <= SpellMaxRangeSq("Shadowburn"))
                            .OrderBy(x => x.Health)
                            .FirstOrDefault() ?? Target;
                    
                        if (Cast("Shadowburn", () => mobsInFrontOfMe < 12, shadowBurnTarget))
                            return true; 
                        if (Cast("Chaos Bolt", () => mobsInFrontOfMe < 6))
                            return true;
                    }

                    if (mobsInFrontOfMe >= 3) {
                        // Apply Immolate to all adds through Cataclysm
                        if (CastSpellOnBestAoETarget("Cataclysm"))
                            return true;
                    }

                    // Priority #3
                    var countAddsInRange = Adds.Count(x => x.DistanceSquaredTo(Target) <= SpellAoERange("Conflagrate"));
                    if (( burningEmbers >= 2 && countAddsInRange > 2 )
                        || ( burningEmbers >= 1 && countAddsInRange >= 8 )) {
                        // Ensure Fire and Brimstone!
                        CastSelf("Fire and Brimstone", () => !HasAura("Fire and Brimstone"));

                        if (CastSpellOnBestAoETarget("Conflagrate"))
                            return true;
                        if (CastSpellOnBestAoETarget(
                                "Immolate",
                                y => !y.HasAura("Immolate") && y.HpLessThanOrElite(0.15)
                            ))
                            return true;
                        if (CastSpellOnBestAoETarget("Incinerate"))
                            return true;
                    }

                    return false;
                }

                /// <inheritdoc/>
                override public void Combat()
                {
                    if (Me.IsCasting && Me.CastingSpellID == (int) WarlockSpellIds.CATACLYSM)
                        return;

                    if (DoSharedRotation())
                        return;

                    int burningEmbers = Me.GetPower(WoWPowerType.WarlockDestructionBurningEmbers);
                    
                    CastSelf("Fire and Brimstone", () => burningEmbers <= 0 && HasAura("Fire and Brimstone"));

                    // MultiTarget Rotation
                    if (Adds.Count > 0 && DoMultitargetRotation(Adds.Count + 1))
                        return;

                    // No Multitarget, so please disable Fire and Brimstone.
                    CastSelf("Fire and Brimstone", () => Me.HasAura("Fire and Brimstone"));

                    // Priority #1
                    if (Cast(
                            "Shadowburn", 
                            () => 
                            Target.HealthFraction <= 0.2
                            && (
                                Me.HasAura("Dark Soul: Instability")
                                || burningEmbers >= 3// No cast time so 4 is good enough!
                                || Target.Health <= 2 * ShadowBurnDamage
                            )
                        
                        ))
                        return;

                    // Priority #2
                    if (( !HasSpell("Cataclysm") || SpellCooldown("Cataclysm") > 0 ) && CastPreventDouble(
                            "Immolate", 
                            () =>
                                ImmolateLock.IsFinished
                            && ( 
                                !Target.HasAura("Immolate", true)
                                || ( Target.AuraTimeRemaining("Immolate") <= 3.5f ) 
                            )
                        ))
                        return;

                    // Refresh with cataclysm if possible
                    if (CastSpellOnBestAoETarget("Cataclysm")) {
                        ImmolateLock.Restart(); // Lock Immolate
                        return;
                    }

                    // Priority #3
                    if (Cast("Conflagrate", () => SpellCharges("Conflagrate") >= 2))
                        return;

                    // Priority #4
                    if (Cast(
                            "Chaos Bolt",
                            () =>
		                       Me.HasAura("Dark Soul: Instability")
                            || burningEmbers >= 3
		                    // because we don't know about .5 fractions of embers... sadly... But it fullifies the T16 4-piece boni
                        ))
                        return;

                    // Priority #5
                    // Refresh Immolate is already done in P#2

                    // Priority #6
                    // TODO: remember old cast position and check with target position and radius so we recast it when he gets out of the rain
                    //if (CastSpellOnBestAoETarget("Rain of Fire", u => !HasAura("Rain of Fire")))
                    //    return;

                    // Priority #7
                    if (Cast("Conflagrate", () => SpellCharges("Conflagrate") >= 2))
                        return;

                    if (Cast("Conflagrate", () => SpellCharges("Conflagrate") == 1))
                        return;

                    // Priority #8
                    if (Cast("Incinerate"))
                        return;
                }
            }
        }
    }
}
