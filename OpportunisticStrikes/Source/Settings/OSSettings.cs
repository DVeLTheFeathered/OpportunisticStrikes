using DVeLsCommonLibrary.UserInterface;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Policy;
using UnityEngine;
using Verse;

namespace OpportunisticStrikes.Settings
{
    public class OSSettings : ModSettings
    {
        protected static int ticksBetweenOSVerbs = ticksBetweenOSVerbsDefault;
        protected static int? ticksBetweenOSVerbsLoaded;
        protected const int ticksBetweenOSVerbsMin = 1;
        protected const int ticksBetweenOSVerbsMax = 120;
        
        protected static float verbChanceAtMelee0 = verbChanceAtMelee0Deault;
        protected static float verbChanceAtMelee20 = verbChanceAtMelee20Default;
        protected const float verbChanceStep = 0.5f;
        
        protected static bool ignoreTooFastAttacks = ignoreTooFastAttacksDefault;
        protected static float tooFastMult = tooFastMultDefault;
        protected static int attackTooFastThreshold;

        protected static bool showOSDPSinStats = true;
        public static bool ShowOSDPSinStats => showOSDPSinStats;

        protected static bool normalizeChanceToBaselineCooldown = normalizeChanceToBaselineCooldownDefault;
        public static bool NormalizeChanceToBaselineCooldown => normalizeChanceToBaselineCooldown;
        protected static int baselineCooldownTicks = baselineCooldownTicksDefault;
        protected const int baselineCooldownTicksMin = 30;
        protected const int baselineCooldownTicksMax = 240;

        protected static List<MeleeAttackResult> interruptions;
        protected static bool interruptOnMiss = interruptOnMissDefault;
        protected static bool interruptOnDodge = interruptOnDodgeDefault;
        protected static bool interruptOnDeflect = interruptOnDeflectDefault;
        protected static bool interruptOnMissPrev = interruptOnMiss;
        protected static bool interruptOnDodgePrev = interruptOnDodge;
        protected static bool interruptOnDeflectPrev = interruptOnDeflect;

        protected static bool markOSVerbsInCombatLog = markOSVerbsInCombatLogDefault;
        public static bool MarkOSVerbsInCombatLog => markOSVerbsInCombatLog;
        protected static string oSVerbMark = OSVerbMarkDefault;
        public static string OSVerbMark => oSVerbMark;

        public const int ticksPerSecond = 60;
        protected float cachedWorkAreaHeight = 0;
        protected Vector2 currentScrollPosition = Vector2.zero;
        protected Listing_Standard listing = new Listing_Standard();
        protected const float checkboxLabelPct = 0.55f;
        protected const float sliderValueLabelPct = 0.04f;
        protected const float sliderNamedValueLabelPct = 0.15f;
        protected const float gapLineHeight = 12;

        #region Default Values

        protected const int ticksBetweenOSVerbsDefault = 12;
        protected const float verbChanceAtMelee0Deault = 0;
        protected const float verbChanceAtMelee20Default = 10;
        protected const bool ignoreTooFastAttacksDefault = true;
        protected const float tooFastMultDefault = 2;
        protected const bool normalizeChanceToBaselineCooldownDefault = true;
        protected const int baselineCooldownTicksDefault = 120;
        protected const bool showOSDPSinStatsDefault = true;
        protected const bool interruptOnMissDefault = true;
        protected const bool interruptOnDodgeDefault = true;
        protected const bool interruptOnDeflectDefault = true;
        protected const bool markOSVerbsInCombatLogDefault = true;
        protected const string OSVerbMarkDefault = "(OS) ";
        
        #endregion Default Values

        public static int TicksBetweenOSVerbs => ticksBetweenOSVerbs;

        public OSSettings()
        {
            listing.maxOneColumn = true;
        }

        public static void RecalculateSettings()
        {
            UpdateVerbs();
            UpdateTooFastAttacks();
            UpdateInterruptions();
            UpdateOSVerbMark();
        }

        protected static void UpdateVerbs()
        {
            // Reminder: Verb uses cached value for TicksBetweenBurstShots, so reload is required
            var osmdef = DefDatabase<ManeuverDef>.GetNamedSilentFail("DVeL_OS_OpportunisticStrikeManeuver");
            if (osmdef != null) osmdef.verb.ticksBetweenBurstShots = ticksBetweenOSVerbs;
        }

        protected static void UpdateTooFastAttacks()
        {
            attackTooFastThreshold = (int)Math.Round(ticksBetweenOSVerbs * tooFastMult);
        }

        protected static void UpdateInterruptions()
        {
            interruptions = new List<MeleeAttackResult>();
            if (interruptOnMiss) interruptions.Add(MeleeAttackResult.Miss);
            if (interruptOnDodge) interruptions.Add(MeleeAttackResult.Dodge);
            if (interruptOnDeflect) interruptions.Add(MeleeAttackResult.Deflect);
        }

        protected static void UpdateOSVerbMark()
        {
            CachedValues.InitializeOSVerbRule();
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            Rect workArea = new Rect(0, 0, inRect.width - 20, cachedWorkAreaHeight); // 20 for slider
            Widgets.BeginScrollView(inRect, ref currentScrollPosition, workArea, true);
            listing.Begin(workArea);

            #region Ticks between combo verbs

            listing.Label("TicksBetweenOSVerbsDescription".Translate(),
                tipSignal: new TipSignal("TicksBetweenOSVerbsTooltip".Translate(), 0f));
            ticksBetweenOSVerbs = Convert.ToInt32(listing.SliderLabeled($"{ticksBetweenOSVerbs}", ticksBetweenOSVerbs, ticksBetweenOSVerbsMin, ticksBetweenOSVerbsMax, sliderValueLabelPct));
            if (ticksBetweenOSVerbs != ticksBetweenOSVerbsLoaded)
                // TODO try "Best".Colorize(Color.green);
                listing.Label("<color=red>" + "NewValueReloadRequiredWarning".Translate() + "</color>");
            listing.GapLine(gapLineHeight);

            #endregion Ticks between combo verbs

            #region Verb Chances Based on Melee Skill

            listing.Label(
                "VerbChanceDescription".Translate(), 
                tipSignal: new TipSignal("VerbChanceTooltip".Translate(), 0f));
            verbChanceAtMelee0 = (float)Math.Round(listing.SliderLabeled("VerbChanceAtMelee0Description".Translate() + $" {verbChanceAtMelee0}%", verbChanceAtMelee0, 0, 100, sliderNamedValueLabelPct) / verbChanceStep, MidpointRounding.AwayFromZero) * verbChanceStep;
            verbChanceAtMelee20 = (float)Math.Round(listing.SliderLabeled("VerbChanceAtMelee20Description".Translate() + $" {verbChanceAtMelee20}%", verbChanceAtMelee20, 0, 100, sliderNamedValueLabelPct) / verbChanceStep, MidpointRounding.AwayFromZero) * verbChanceStep;
            verbChanceAtMelee0 = Math.Min(verbChanceAtMelee0, verbChanceAtMelee20);
            listing.GapLine(gapLineHeight);

            #endregion Verb Chances Based on Melee Skill

            #region Ignore Combo for fast weapons

            listing.CheckboxLabeled("IgnoreTooFastAttacksDescription".Translate(), ref ignoreTooFastAttacks, labelPct: checkboxLabelPct);
            
            if (ignoreTooFastAttacks) {
                GameFont oldFont = Text.Font;
                Text.Font = GameFont.Tiny;
                listing.Label("WhatIsTooFastDescription".Translate());
                Text.Font = oldFont;
                tooFastMult = (float)Math.Round(listing.SliderLabeled("MultDescription".Translate() + $"{tooFastMult}", tooFastMult, 1, 10, sliderNamedValueLabelPct), 1);
                UpdateTooFastAttacks();
            }
            listing.GapLine(gapLineHeight);
            
            #endregion Ignore Combo for fast weapons

            #region Show OS DPS in Stats screen

            listing.CheckboxLabeled("ShowOSDPSinStats".Translate(), ref showOSDPSinStats, labelPct: checkboxLabelPct);
            listing.GapLine(gapLineHeight);
            
            #endregion Show OS DPS in Stats screen

            #region Normalize Attack Speed

            listing.CheckboxLabeled("NormalizeChanceToBaselineCooldown".Translate(), ref normalizeChanceToBaselineCooldown, 
                tooltip: "NormalizationExplanation".Translate(),
                labelPct: checkboxLabelPct);
            if (normalizeChanceToBaselineCooldown)
            {
                listing.Label("BaselineCooldown".Translate());
                baselineCooldownTicks = Convert.ToInt32(listing.SliderLabeled($"{baselineCooldownTicks}", baselineCooldownTicks, baselineCooldownTicksMin, baselineCooldownTicksMax, sliderValueLabelPct));
            }
            listing.GapLine(gapLineHeight);

            #endregion Normalize Attack Speed

            #region Interruptions

            listing.Label("YourAttackInterruptionsDescription".Translate());
            interruptOnMissPrev = interruptOnMiss;
            interruptOnDodgePrev = interruptOnDodge;
            interruptOnDeflectPrev = interruptOnDeflect;
            listing.CheckboxLabeled("InterruptionMiss".Translate(), ref interruptOnMiss, labelPct: checkboxLabelPct);
            listing.CheckboxLabeled("InterruptionDodge".Translate(), ref interruptOnDodge, labelPct: checkboxLabelPct);
            listing.CheckboxLabeled("InterruptionDeflect".Translate(), ref interruptOnDeflect, labelPct: checkboxLabelPct);
            
            if (interruptOnMissPrev != interruptOnMiss
                || interruptOnDodgePrev != interruptOnDodge
                || interruptOnDeflectPrev != interruptOnDeflect) UpdateInterruptions();

            listing.GapLine(gapLineHeight);
            
            #endregion Interruptions

            #region Mark OS in Combat Log

            listing.CheckboxLabeled("MarkOSVerbsInCombatLog".Translate(), ref markOSVerbsInCombatLog, labelPct: checkboxLabelPct);
            if (markOSVerbsInCombatLog)
            {
                GameFont oldFont = Text.Font;
                Text.Font = GameFont.Tiny;
                listing.Label("VerbMarkInCombatLogComment".Translate());
                Text.Font = oldFont;
                string oSVerbMarkPrev = oSVerbMark;
                oSVerbMark = listing.TextEntryLabeled("VerbMarkInCombatLog".Translate(), OSVerbMark, 0.25f);
                oSVerbMark = OSVerbMark.Replace("[", "").Replace("]", "");
                listing.Label(OSVerbMark + "ExampleCombatLogMessage".Translate());
                if (oSVerbMark != oSVerbMarkPrev) UpdateOSVerbMark();
            }
            listing.GapLine(gapLineHeight);
            
            #endregion Mark OS in Combat Log

            listing.End();
            Widgets.EndScrollView();
            cachedWorkAreaHeight = listing.CurHeight;
        }

        public static bool InterruptingResult(MeleeAttackResult result) => interruptions.Contains(result);

        /// <summary>
        /// Result is in "Rimworld"-style percents (1% returns 0.01f)
        /// </summary>
        public static float VerbChanceFromSkill(int meleeSkill) => (verbChanceAtMelee0 + (verbChanceAtMelee20 - verbChanceAtMelee0) * meleeSkill / 20) / 100;

        /// <param name="cooldown">Cooldown in ticks</param>
        /// <returns>Multiplier to the verb chance</returns>
        public static float VerbChanceNormalizedMult(int cooldown)
        {
            return normalizeChanceToBaselineCooldown
                ? (float)cooldown / baselineCooldownTicks
                : 1;
        }

        public static bool VerbTooFast(Verb_MeleeAttack verb)
        {
            return ignoreTooFastAttacks && verb.verbProps.AdjustedCooldownTicks(verb, verb.CasterPawn) <= attackTooFastThreshold;
        }

        public void ResetToDefaults()
        {
            // TODO check before release
            ticksBetweenOSVerbs = ticksBetweenOSVerbsDefault;

            verbChanceAtMelee0 = verbChanceAtMelee0Deault;
            verbChanceAtMelee20 = verbChanceAtMelee20Default;

            ignoreTooFastAttacks = ignoreTooFastAttacksDefault;
            tooFastMult = tooFastMultDefault;
            
            interruptOnMiss = interruptOnMissDefault;
            interruptOnDodge = interruptOnDodgeDefault;
            interruptOnDeflect = interruptOnDeflectDefault;
            interruptOnMissPrev = interruptOnMiss;
            interruptOnDodgePrev = interruptOnDodge;
            interruptOnDeflectPrev = interruptOnDeflect;

            markOSVerbsInCombatLog = markOSVerbsInCombatLogDefault;
            oSVerbMark = OSVerbMarkDefault;
        }

        public override void ExposeData()
        {
            // TODO do not forget to save
            base.ExposeData();
            Scribe_Values.Look(ref ticksBetweenOSVerbs, "ticksBetweenOSVerbs", ticksBetweenOSVerbsDefault);
            if (!ticksBetweenOSVerbsLoaded.HasValue) ticksBetweenOSVerbsLoaded = ticksBetweenOSVerbs;
            
            Scribe_Values.Look(ref verbChanceAtMelee0, "verbChanceAtMelee0", verbChanceAtMelee0Deault);
            Scribe_Values.Look(ref verbChanceAtMelee20, "verbChanceAtMelee20", verbChanceAtMelee20Default);
            
            Scribe_Values.Look(ref ignoreTooFastAttacks, "ignoreTooFastAttacks", ignoreTooFastAttacksDefault);
            Scribe_Values.Look(ref tooFastMult, "tooFastMult", tooFastMultDefault);
            
            Scribe_Values.Look(ref showOSDPSinStats, "showOSDPSinStats", showOSDPSinStatsDefault);
            
            Scribe_Values.Look(ref normalizeChanceToBaselineCooldown, "normalizeChanceToBaselineCooldown", normalizeChanceToBaselineCooldownDefault);
            Scribe_Values.Look(ref baselineCooldownTicks, "baselineCooldownTicks", baselineCooldownTicksDefault);
            
            Scribe_Values.Look(ref interruptOnMiss, "interruptOnMiss", interruptOnMissDefault);
            Scribe_Values.Look(ref interruptOnDodge, "interruptOnDodge", interruptOnDodgeDefault);
            Scribe_Values.Look(ref interruptOnDeflect, "interruptOnDeflect", interruptOnDeflectDefault);
            
            Scribe_Values.Look(ref markOSVerbsInCombatLog, "MarkOSVerbsInCombatLog", markOSVerbsInCombatLogDefault);
            Scribe_Values.Look(ref oSVerbMark, "OSVerbMark", OSVerbMarkDefault);
        }
    }
}
