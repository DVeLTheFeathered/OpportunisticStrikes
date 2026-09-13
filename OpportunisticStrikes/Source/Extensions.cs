using OpportunisticStrikes.Settings;
using RimWorld;
using Verse;

namespace OpportunisticStrikes
{
    public static class Extensions
    {
        public static Verb_OpportunisticStrike PawnOSVerb(this Pawn pawn) =>
            pawn?.verbTracker.AllVerbs.FirstOrDefault(x => x is Verb_OpportunisticStrike) as Verb_OpportunisticStrike;
        
        public static bool PawnCanOS(this Pawn pawn) => !(pawn?.PawnOSVerb() is null);

        public static Verb_OpportunisticStrike PawnOSVerb(this Thing thing) => (thing as Pawn).PawnOSVerb();
        public static bool PawnCanOS(this Thing thing) => (thing as Pawn).PawnCanOS();

        public static float VerbChanceInCombo(this Verb verb)
        {
            if (!(verb is Verb_MeleeAttack)) return 0;
            int meleeSkill = verb.CasterPawn?.skills?.GetSkill(SkillDefOf.Melee)?.GetLevel() ?? 0;
            return OSSettings.VerbChanceFromSkill(meleeSkill) * (verb.tool?.chanceFactor ?? 1);
        }

    }
}
