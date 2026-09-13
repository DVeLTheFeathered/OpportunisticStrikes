using Verse;

namespace OpportunisticStrikes.Settings
{
    [StaticConstructorOnStartup]
    public static class ModInit
    {
        static ModInit()
        {
            OSSettings.RecalculateSettings();
        }
    }
}
