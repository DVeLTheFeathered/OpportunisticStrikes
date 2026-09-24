using OpportunisticStrikes.Settings;
using Verse;
using Verse.Grammar;

namespace OpportunisticStrikes
{
    public static class CachedValues
    {
        public static LogEntryDef MeleeAttackCombo { get; private set; }

        public static MentalStateDef SadisticRage { get; private set; }

        public static Rule_String OSRule { get; private set; }

        public static void InitializeCachedValues()
        {
            const string meleeAttackComboLogDefName = "DVeL_OS_MeleeAttackComboLog";
            const string sadisticRageDefName = "SadisticRage";

            MeleeAttackCombo = DefDatabase<LogEntryDef>.GetNamed(meleeAttackComboLogDefName);
            SadisticRage = DefDatabase<MentalStateDef>.GetNamed(sadisticRageDefName);

            InitializeOSVerbRule();
        }

        public static void InitializeOSVerbRule()
        {
            OSRule = new Rule_String($"r_logentry(priority=1000, uses=1)->{OSSettings.OSVerbMark}[r_logentry]");
        }

    }
}
