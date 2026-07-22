using HarmonyLib;
using Verse;

namespace TestMod.HarmonyPatches.Verb
{
    
    [HarmonyPatch(typeof(Verse.Verb), "TryCastNextBurstShot")]
    public static class TryCastNextBurstShotPatch
    {
        // TODO should check compatibility with DualWield
        // https://github.com/MemeGoddess/DualWield/tree/master/Source/Harmony
        public static void Postfix(Verse.Verb __instance)
        {
            #region Exit if situation is unrelated to AoO
            if (!(__instance is RimWorld.Verb_MeleeAttack initiatingVerb)) return; // melee attack
            if (initiatingVerb.CurrentTarget.Pawn is null) return; // melee attack against pawn
            var aoo = initiatingVerb.CasterPawn?.verbTracker.AllVerbs.FirstOrDefault(x => x is Verb_AttackOfOpportunity) as Verb_AttackOfOpportunity;
            if (aoo is null) return; // melee attack against pawn by someone who can do FollowUp
            #endregion Exit if situation is unrelated to AoO

            aoo.FollowUpLogic(initiatingVerb);
        }
        
    }
    
}
