using AttacksOfOpportunity.Settings;
using System.Runtime;
using Verse;

namespace AttacksOfOpportunity.Settings
{
    public class AttacksOfOpportunityMod : Mod
    {
        public static AttacksOfOpportunityModSettings settings;

        public AttacksOfOpportunityMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<AttacksOfOpportunityModSettings>();
        }

        public override string SettingsCategory()
        {
            return "Attacks Of Opportunity";
        }
    }
}
