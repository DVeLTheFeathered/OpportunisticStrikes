using Verse;
using Verse.Grammar;

namespace TestMod
{
    [StaticConstructorOnStartup]
    public static class CachedValues
    {
        private const string meleeAttackComboDefName = "DVeL_AoO_MeleeAttackCombo";
        private const string sadisticRageDefName = "SadisticRage";
        private const string aoOLogRuleString = "r_logentry(priority=1000, uses=1)->(AoO) [r_logentry]";

        public static LogEntryDef MeleeAttackCombo { get; }

        public static MentalStateDef SadisticRage { get; }

        public static Rule_String AoORule { get; }

        static CachedValues()
        {
            MeleeAttackCombo = DefDatabase<LogEntryDef>.GetNamed(meleeAttackComboDefName);
            SadisticRage = DefDatabase<MentalStateDef>.GetNamed(sadisticRageDefName);
            AoORule = new Rule_String(aoOLogRuleString);

            string errorCause = 
                MeleeAttackCombo is null ? meleeAttackComboDefName :
                SadisticRage is null ? sadisticRageDefName :
                AoORule is null ? nameof(AoORule)
                : null;
            if (!(errorCause is null))
                Log.Error($"AttacksOfOpportunity: Failed to initialize {errorCause} in static constructor");
        }

    }
}
