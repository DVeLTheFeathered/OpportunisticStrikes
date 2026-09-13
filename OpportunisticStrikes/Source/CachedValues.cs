using Verse;
using Verse.Grammar;

namespace OpportunisticStrikes
{
    [StaticConstructorOnStartup]
    public static class CachedValues
    {
        private const string meleeAttackComboLogDefName = "DVeL_OS_MeleeAttackComboLog";
        private const string sadisticRageDefName = "SadisticRage";
        private const string aoOLogRuleString = "r_logentry(priority=1000, uses=1)->(OS) [r_logentry]";

        public static LogEntryDef MeleeAttackCombo { get; }

        public static MentalStateDef SadisticRage { get; }

        public static Rule_String AoORule { get; }

        static CachedValues()
        {
            MeleeAttackCombo = DefDatabase<LogEntryDef>.GetNamed(meleeAttackComboLogDefName);
            SadisticRage = DefDatabase<MentalStateDef>.GetNamed(sadisticRageDefName);
            AoORule = new Rule_String(aoOLogRuleString);

            string errorCause = 
                MeleeAttackCombo is null ? meleeAttackComboLogDefName :
                SadisticRage is null ? sadisticRageDefName :
                AoORule is null ? nameof(AoORule)
                : null;
            if (!(errorCause is null))
                Log.Error($"OpportunisticStrikes: Failed to initialize {errorCause} in static constructor");
        }

    }
}
