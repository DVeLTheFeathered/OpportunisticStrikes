using DVeLsCommonLibrary;
using LudeonTK;
using OpportunisticStrikes;
using OpportunisticStrikes.Settings;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DVeLsCommonLibrary.UserInterface;
using UnityEngine;
using Verse;

namespace OpportunisticStrikes.Stats
{
    public class StatWorker_OSDPS : StatWorker, ICustomDrawStat
    {
        public override bool IsDisabledFor(Thing thing)
        {
            return !thing.PawnCanOS() || base.IsDisabledFor(thing) || StatDefOf.MeleeHitChance.Worker.IsDisabledFor(thing);
        }

        public override bool ShouldShowFor(StatRequest req)
        {
            return OSSettings.ShowOSDPSinStats && req.Thing.PawnCanOS() && base.ShouldShowFor(req);
        }

        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            return OSSettings.InterruptingResult(MeleeAttackResult.Miss) ? DPS(req) : DPSNoInterruptions(req);

            #region *Sad math noises*
            /*
            // We have 3 verbs, we do 100 strikes.

            float hitChance = 0.82f;
            float v1Damage = 10;
            float v2Damage = 5;
            float v3Damage = 1;
            float v1Chance = 0.7f; // Chance to be chosen for attach, sum = 1;
            float v2Chance = 0.2f;
            float v3Chance = 0.1f;
            float v1OSChance = 0.9f;
            float v2OSChance = 0.5f;
            float v3OSChance = 0.2f;
            float v1Cooldown = 1;
            float v2Cooldown = 2;
            float v3Cooldown = 1;
            float ticksBetweenComboVerbs = 12;
            float comboCooldown = ticksBetweenComboVerbs / 60;
            
            float TotalDamage;
            float AverageDamage;
            float TotalTime;
            float ComboTime;
            float AverageCooldown;
            float DPS;

            // Basic melee strike cycle: deal damage, wait cooldown. 
            // 70 of them will be v1, 25 - v2 and 10 - v3. Then divide by 100 to find average.
            TotalDamage = (v1Damage * v1Chance * 100 + v2Damage * v2Chance * 100 + v3Damage * v3Chance * 100) * hitChance;
            TotalDamage = (v1Damage * v1Chance + v2Damage * v2Chance + v3Damage * v3Chance) * 100 * hitChance;
            AverageDamage = TotalDamage / 100;
            AverageDamage = (v1Damage * v1Chance + v2Damage * v2Chance + v3Damage * v3Chance) * hitChance;
            
            TotalTime = (v1Cooldown * v1Chance * 100 + v2Cooldown * v2Chance * 100 + v3Cooldown * v3Chance * 100);
            TotalTime = (v1Cooldown * v1Chance + v2Cooldown * v2Chance + v3Cooldown * v3Chance) * 100;
            AverageCooldown = TotalTime / 100;
            AverageCooldown = v1Cooldown * v1Chance + v2Cooldown * v2Chance + v3Cooldown * v3Chance;

            DPS = AverageDamage / AverageCooldown;

            // Melee strike with FollowUp cycle: Deal damage, deal random number of OS, wait cooldown.
            // Or: Deal full list of attacks (but with some chance) each cycle, because initiating verb can not be repeated

            // Assuming all have 100 chance to followUp
            TotalDamage = (v1Damage + v2Damage + v3Damage) * 100;
            // Now, with their OSChance in combo:
            TotalDamage = (v1Damage * v1OSChance + v2Damage * v1OSChance + v3Damage * v1OSChance) * 100;
            // But each combo will have one initiating verb, which means its vOSChance is overriden with 1f;
            // Lets say v1 is initiating in 100% cases. Then 
            TotalDamage = (v1Damage + v2Damage * v1OSChance + v3Damage * v1OSChance) * 100;
            // Modify it from prev formula instead
            TotalDamage = (v1Damage * v1OSChance + v2Damage * v1OSChance + v3Damage * v1OSChance + v1Damage * (1 - v1OSChance)) * 100;
            // It means initiating verb gives (+ vDamage * (1 - vOSChance)) bonus damage with each strike
            // Now, we have each verb chance to initiate. In 100 strikes each bonus will activate vChance * 100 times. 70 for v1 etc.
            TotalDamage = (v1Damage * v1OSChance
                           + v2Damage * v1OSChance
                           + v3Damage * v1OSChance
                           + v1Damage * v1Chance * (1 - v1OSChance)
                           + v2Damage * v2Chance * (1 - v2OSChance)
                           + v3Damage * v3Chance * (1 - v3OSChance))
                          * 100;
            var p = v1Damage * v1OSChance + v1Damage * v1Chance * (1 - v1OSChance);
            p = v1Damage * (v1OSChance + v1Chance * (1 - v1OSChance));
            p = v1Damage * (v1OSChance + v1Chance  - v1Chance * v1OSChance);
            TotalDamage = (v1Damage * (v1OSChance + v1Chance - v1Chance * v1OSChance)
                           + v2Damage * (v2OSChance + v2Chance - v2Chance * v2OSChance)
                           + v3Damage * (v3OSChance + v3Chance - v3Chance * v3OSChance)) 
                          * 100;

            // Previous formula assumes that all attacks hit.
            // If combo can not be interrupted by miss
            TotalDamage = (v1Damage * (v1OSChance + v1Chance - v1Chance * v1OSChance)
                           + v2Damage * (v2OSChance + v2Chance - v2Chance * v2OSChance)
                           + v3Damage * (v3OSChance + v3Chance - v3Chance * v3OSChance))
                          * 100 * hitChance;

            // But what if it can? We ignore enemy dodge and deflect in DPS, meaning we must factor in only hitChance
            // Each subsequent attack has less chance to strike. 
            // Previous formula shows that each verb contribution to total damage can be calculated independently.
            p = v1Damage * v1OSChance + v1Damage * v1Chance * (1 - v1OSChance);
            // First part is from being in combo in general, second is bonus from being initiating verb;
            // It means we can calculate damage contribution ignoring initiating verb factor, and then add it later

            // Assuming all verbs have 100% chance to followUp and random order in combo.
            // We have VerbsTotal attacks. Inside combo order is random, so each attack will be first/second/third/etc. (1 / VerbsTotal) times
            float VerbsTotal = 3;
            float hitChance2 = hitChance * hitChance;
            float hitChance3 = hitChance * hitChance * hitChance; // Up to VerbsTotal

            TotalDamage = v1Damage * 100 / VerbsTotal * hitChance + v1Damage * 100 / VerbsTotal * hitChance2 + v1Damage * 100 / VerbsTotal * hitChance3
                          + v2Damage * 100 / VerbsTotal * hitChance + v2Damage * 100 / VerbsTotal * hitChance2 + v2Damage * 100 / VerbsTotal * hitChance3
                          + v3Damage * 100 / VerbsTotal * hitChance + v3Damage * 100 / VerbsTotal * hitChance2 + v3Damage * 100 / VerbsTotal * hitChance3;
            TotalDamage = v1Damage * 100 / VerbsTotal * (hitChance + hitChance2 + hitChance3)
                          + v2Damage * 100 / VerbsTotal * (hitChance + hitChance2 + hitChance3)
                          + v3Damage * 100 / VerbsTotal * (hitChance + hitChance2 + hitChance3);
            // Now with vOSChance
            TotalDamage = v1Damage * v1OSChance * 100 / VerbsTotal * (hitChance + hitChance2 + hitChance3)
                          + v2Damage * v2OSChance * 100 / VerbsTotal * (hitChance + hitChance2 + hitChance3)
                          + v3Damage * v3OSChance * 100 / VerbsTotal * (hitChance + hitChance2 + hitChance3);

            // Add an initiating bonus (always for first attack, so hitChance applies once)
            TotalDamage = (v1Damage * v1OSChance / VerbsTotal * (hitChance + hitChance2 + hitChance3)
                           + v2Damage * v2OSChance / VerbsTotal * (hitChance + hitChance2 + hitChance3)
                           + v3Damage * v3OSChance / VerbsTotal * (hitChance + hitChance2 + hitChance3)
                           + v1Damage * v1Chance * (1 - v1OSChance) * hitChance
                           + v2Damage * v2Chance * (1 - v2OSChance) * hitChance
                           + v3Damage * v3Chance * (1 - v3OSChance) * hitChance
                          ) * 100;
            p = v1Damage * v1OSChance / VerbsTotal * (hitChance + hitChance2 + hitChance3) + v1Damage * v1Chance * (1 - v1OSChance) * hitChance;
            p = v1Damage * (v1OSChance / VerbsTotal * (hitChance + hitChance2 + hitChance3) + v1Chance * (1 - v1OSChance) * hitChance);

            TotalDamage = (v1Damage * (v1OSChance / VerbsTotal * (hitChance + hitChance2 + hitChance3) + v1Chance * (1 - v1OSChance) * hitChance)
                          + v2Damage * (v2OSChance / VerbsTotal * (hitChance + hitChance2 + hitChance3) + v2Chance * (1 - v2OSChance) * hitChance)
                          + v3Damage * (v3OSChance / VerbsTotal * (hitChance + hitChance2 + hitChance3) + v3Chance * (1 - v3OSChance) * hitChance)
                          ) * 100;
            // Seems like damage is done.
            AverageDamage = TotalDamage / 100;
            AverageDamage = v1Damage * (v1OSChance / VerbsTotal * (hitChance + hitChance2 + hitChance3) + v1Chance * (1 - v1OSChance) * hitChance)
                            + v2Damage * (v2OSChance / VerbsTotal * (hitChance + hitChance2 + hitChance3) + v2Chance * (1 - v2OSChance) * hitChance)
                            + v3Damage * (v3OSChance / VerbsTotal * (hitChance + hitChance2 + hitChance3) + v3Chance * (1 - v3OSChance) * hitChance);


            // Now to cooldown time.
            // We have one "main" cooldown from initiating strike. And each combo attack in cycle adds comboCooldown.
            // Assuming all verbs have 100% in combo

            TotalTime = (v1Cooldown * v1Chance + v2Cooldown * v2Chance + v3Cooldown * v3Chance) * 100
                + comboCooldown * (VerbsTotal - 1) * 100;

            // The first part will not change, but the second depends on hitChance, amount of verbs in combo and their vOSChance.

            // Lets do same trick as with damage - treat each cycle as combo of all attacks
            // Assuming all verbs have 100% in combo
            ComboTime = comboCooldown * (VerbsTotal) * 100;
            // But each strike has a chance to be selected
            ComboTime = comboCooldown * (v1OSChance + v2OSChance + v3OSChance) * 100;
            // Now we have to remove the initiating verb from combo, because it contributes in first part
            // with vChance to be selected as initiating, each verb will contribute to combo in (1 - vChance) cases
            ComboTime = comboCooldown * (v1OSChance * (1 - v1Chance) + v2OSChance * (1 - v2Chance) + v3OSChance * (1 - v3Chance)) * 100;
            // If hitChance does not interrupt the combo, then this is it. But if it does... 
            // Initiating verb contributes to first part and is removed from equation, so we have (VerbsTotal - 1) verbs in followUp
            // Each subsequent chance is multiplied by hitChance
            // v1 contribution without interruptions
            p = comboCooldown * v1OSChance * (1 - v1Chance) * 100;
            // v1 is always second in followUp
            p = comboCooldown * v1OSChance * (1 - v1Chance) * 100 * hitChance;
            // v1 is always third in followUp
            p = comboCooldown * v1OSChance * (1 - v1Chance) * 100 * hitChance2;
            // etc. until (VerbsTotal - 1)
            // Each verb can be second/third/etc. in 100 / (VerbsTotal - 1) times.
            p = comboCooldown * v1OSChance * (1 - v1Chance) * 100 * hitChance / (VerbsTotal - 1) 
                + comboCooldown * v1OSChance * (1 - v1Chance) * 100 * hitChance2 / (VerbsTotal - 1);
            p = comboCooldown * v1OSChance * (1 - v1Chance) * 100 * (hitChance / (VerbsTotal - 1) + hitChance2 / (VerbsTotal - 1));
            p = comboCooldown * v1OSChance * (1 - v1Chance) * 100 * (hitChance  + hitChance2) / (VerbsTotal - 1);

            // Applying it to each verb in followUp
            ComboTime = comboCooldown * (v1OSChance * (1 - v1Chance) + v2OSChance * (1 - v2Chance) + v3OSChance * (1 - v3Chance)) * (hitChance + hitChance2) / (VerbsTotal - 1) * 100;

            TotalTime = (v1Cooldown * v1Chance + v2Cooldown * v2Chance + v3Cooldown * v3Chance) * 100
                        + ComboTime;
            TotalTime = (v1Cooldown * v1Chance
                         + v2Cooldown * v2Chance
                         + v3Cooldown * v3Chance
                        + comboCooldown * (v1OSChance * (1 - v1Chance) + v2OSChance * (1 - v2Chance) + v3OSChance * (1 - v3Chance)) * (hitChance + hitChance2) / (VerbsTotal - 1)
                        ) * 100;

            AverageCooldown = TotalTime / 100;
            AverageCooldown = v1Cooldown * v1Chance
                              + v2Cooldown * v2Chance
                              + v3Cooldown * v3Chance
                              + comboCooldown * (v1OSChance * (1 - v1Chance) + v2OSChance * (1 - v2Chance) + v3OSChance * (1 - v3Chance)) * (hitChance + hitChance2) / (VerbsTotal - 1);

            DPS = AverageDamage / AverageCooldown;
            */
            #endregion *Sad math noises*

        }

        protected float DPS(StatRequest req)
        {
            Pawn pawn = req.Thing as Pawn;
            if (!pawn.PawnCanOS()) return 0;
            List<VerbEntry> verbEntries = pawn.meleeVerbs.GetUpdatedAvailableVerbsList(false);
            if (verbEntries.Count == 0) return 0;
            float AverageDamage = 0;
            float AverageCooldown = 0;
            float hitChance = pawn.GetStatValue(StatDefOf.MeleeHitChance);
            if (verbEntries.Count == 1) { // one verb means there is no "combo contribution" and messes with calculations
                Verb verb = verbEntries[0].verb;
                AverageDamage = verb.verbProps.AdjustedMeleeDamageAmount(verb, pawn) * hitChance;
                AverageCooldown = verb.verbProps.AdjustedCooldownTicks(verb, pawn) / (float)OSSettings.ticksPerSecond;
                return AverageCooldown != 0 ? AverageDamage / AverageCooldown : 0;
            }
            float hitChanceAlmostFull = 0; // sum of degrees of hitChance up to VerbAmount - 1
            float hcd = 1;
            int VerbsTotal = verbEntries.Count;
            for (int i = 1; i < VerbsTotal; i++) {
                hcd *= hitChance;
                hitChanceAlmostFull += hcd;
            }
            float hitChanceFull = hitChanceAlmostFull + hcd * hitChance;
            float comboCooldown = OSSettings.TicksBetweenOSVerbs / (float)OSSettings.ticksPerSecond;

            // Added "normalize chance to baseline cooldown's value" option after I was done with all the math here, and have zero desire to redo it. So lets hope that this will be close to actual numbers...
            float verbChanceMult = AverageChanceNormalizedMult(verbEntries);
            
            #region See GetValueUnfinalized

            //AverageDamage = v1Damage * (v1OSChance / VerbsTotal * (hitChance + hitChance2 + hitChance3) + v1Chance * (1 - v1OSChance) * hitChance)
            //                + v2Damage * (v2OSChance / VerbsTotal * (hitChance + hitChance2 + hitChance3) + v2Chance * (1 - v2OSChance) * hitChance)
            //                + v3Damage * (v3OSChance / VerbsTotal * (hitChance + hitChance2 + hitChance3) + v3Chance * (1 - v3OSChance) * hitChance);
            //AverageCooldown = v1Cooldown * v1Chance
            //                  + v2Cooldown * v2Chance
            //                  + v3Cooldown * v3Chance
            //                  + comboCooldown * (v1OSChance * (1 - v1Chance) + v2OSChance * (1 - v2Chance) + v3OSChance * (1 - v3Chance)) * (hitChance + hitChance2) / (VerbsTotal - 1);

            #endregion See GetValueUnfinalized

            foreach (VerbEntry verbEntry in verbEntries) {
                Verb verb = verbEntry.verb;
                float verbSelectionChance = verbEntry.GetSelectionWeight(null);
                float verbOSChance = verb.VerbChanceInCombo() * verbChanceMult;
                AverageDamage += verb.verbProps.AdjustedMeleeDamageAmount(verb, pawn)
                                 * (verbOSChance / VerbsTotal * hitChanceFull + verbSelectionChance * (1 - verbOSChance) * hitChance);
                AverageCooldown += verb.verbProps.AdjustedCooldownTicks(verb, pawn) / (float)OSSettings.ticksPerSecond * verbSelectionChance;
                AverageCooldown += comboCooldown * verbOSChance * (1 - verbSelectionChance) * hitChanceAlmostFull / (VerbsTotal - 1);
            }

            return AverageCooldown != 0 ? AverageDamage / AverageCooldown : 0;
        }

        protected float DPSNoInterruptions(StatRequest req)
        {
            Pawn pawn = req.Thing as Pawn;
            if (!pawn.PawnCanOS()) return 0;
            List<VerbEntry> verbEntries = pawn.meleeVerbs.GetUpdatedAvailableVerbsList(false);
            if (verbEntries.Count == 0) return 0;
            float AverageDamage = 0;
            float AverageCooldown = 0;
            float hitChance = pawn.GetStatValue(StatDefOf.MeleeHitChance);
            float comboCooldown = OSSettings.TicksBetweenOSVerbs / (float)OSSettings.ticksPerSecond;

            // Added "normalize chance to baseline cooldown's value" option after I was done with all the math here, and have zero desire to redo it. So lets hope that this will be close to actual numbers...
            float verbChanceMult = AverageChanceNormalizedMult(verbEntries);

            #region See GetValueUnfinalized

            // AvgDamage = (v1Damage * (v1OSChance + v1Chance - v1Chance * v1OSChance)
            //               + v2Damage * (v2OSChance + v2Chance - v2Chance * v2OSChance)
            //               + v3Damage * (v3OSChance + v3Chance - v3Chance * v3OSChance))
            //              * hitChance;
            // AvgComboTime = (v1OSChance * (1 - v1Chance) + v2OSChance * (1 - v2Chance) + v3OSChance * (1 - v3Chance))
            //                * comboCooldown;
            // AvgTime = (v1Cooldown * v1Chance + v2Cooldown * v2Chance + v3Cooldown * v3Chance) + AvgComboTime;

            #endregion See GetValueUnfinalized

            foreach (VerbEntry verbEntry in verbEntries) {
                Verb verb = verbEntry.verb;
                float verbSelectionChance = verbEntry.GetSelectionWeight(null);
                float verbOSChance = verb.VerbChanceInCombo() * verbChanceMult;
                AverageDamage += verb.verbProps.AdjustedMeleeDamageAmount(verb, pawn)
                                 * (verbSelectionChance + verbOSChance - verbSelectionChance * verbOSChance);
                AverageCooldown += verb.verbProps.AdjustedCooldownTicks(verb, pawn) / (float)OSSettings.ticksPerSecond * verbSelectionChance;
                AverageCooldown += verbOSChance * (1 - verbSelectionChance) * comboCooldown;
            }
            AverageDamage *= hitChance;

            return AverageCooldown != 0 ? AverageDamage / AverageCooldown : 0;
        }

        public float CustomDraw(Rect rect, StatRequest request)
        {
            if (!(request.Thing is Pawn pawn && pawn.PawnCanOS())) return 0;
            List<VerbEntry> verbEntries = pawn.meleeVerbs.GetUpdatedAvailableVerbsList(false);
            List<Verb> allMeleeVerbs = verbEntries
                .Select(x => x.verb)
                .Where(x => !(x is Verb_OpportunisticStrike))
                .ToList();

            // Added "normalize chance to baseline cooldown's value" option after I was done with all the math here, and have zero desire to redo it. So lets hope that this will be close to actual numbers...
            float verbChanceMult = AverageChanceNormalizedMult(verbEntries);
            List<TableDataGetter<Verb>> tableDataGetterList = new List<TableDataGetter<Verb>>
            {
                //new TableDataGetter<Verb>("Chance",  v => (v.VerbChanceInCombo() * 100).ToString("0.##") + "%"), // Somehow this line fries ReSharper brains...
                new TableDataGetter<Verb>("ColumnHeader_Chance".Translate(), v => $"{v.VerbChanceInCombo() * verbChanceMult * 100:0.##}%"),
                new TableDataGetter<Verb>("ColumnHeader_Damage".Translate(),  v => v.verbProps.AdjustedMeleeDamageAmount(v,pawn).ToString("0.##")),
                new TableDataGetter<Verb>("ColumnHeader_Tool".Translate(),  v => v.tool != null ? v.tool.label : ""),
                new TableDataGetter<Verb>("ColumnHeader_Source".Translate(), v =>
                {
                    if (v.HediffSource != null) return "SourceType_Hediff".Translate() + ": " + v.HediffSource.Label;
                    if (v.EquipmentSource != null) return "SourceType_Equipment".Translate() + ": " + v.EquipmentSource.LabelNoParenthesisCap;
                    if (v.DirectOwner is Pawn verbOwner) return "SourceType_Body".Translate() + ": " + verbOwner.RaceProps.body.LabelCap;
                    return "SourceType_Unknown".Translate();
                })
            };

            Rect tableRect = Drawing.DrawTable(rect, allMeleeVerbs, tableDataGetterList);

            return tableRect.height;
            // return Drawing.DrawTable(rect, allMeleeVerbs, tableDataGetterList).height; // Also fries ReSharper brain...
        }

        protected float AverageChanceNormalizedMult(List<VerbEntry> verbEntries)
        {
            if (!OSSettings.NormalizeChanceToBaselineCooldown) return 1;
            float avgCooldown = 0;
            foreach (var verbEntry in verbEntries)
            {
                Verb verb = verbEntry.verb;
                avgCooldown += verbEntry.GetSelectionWeight(null) * verb.verbProps.AdjustedCooldownTicks(verb, verb.CasterPawn);
            }
            return OSSettings.VerbChanceNormalizedMult((int)Math.Round(avgCooldown));
        }
    }
}
