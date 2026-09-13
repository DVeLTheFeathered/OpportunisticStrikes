using UnityEngine;
using Verse;

namespace OpportunisticStrikes.Settings
{
    public class OSMod : Mod
    {
        public static OSSettings settings;

        public OSMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<OSSettings>();
        }

        public override string SettingsCategory()
        {
            return "OpportunisticStrikesModName".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.DoSettingsWindowContents(inRect);
        }
    }
}
