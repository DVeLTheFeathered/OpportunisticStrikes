using HarmonyLib;
using OpportunisticStrikes.Settings;
using Verse;

namespace OpportunisticStrikes.HarmonyPatches.Verb
{
    
    [HarmonyPatch(typeof(Verse.Verb), "TryCastNextBurstShot")]
    public static class TryCastNextBurstShotPatch
    {
        // TODO should check compatibility with DualWield
        // https://github.com/MemeGoddess/DualWield/tree/master/Source/Harmony
        public static void Postfix(Verse.Verb __instance)
        {
            #region Exit if situation is unrelated to OS
            if (!(__instance is RimWorld.Verb_MeleeAttack initiatingVerb)) return; // melee attack
            if (initiatingVerb.CurrentTarget.Pawn is null) return; // melee attack against pawn
            if (OSSettings.VerbTooFast(initiatingVerb)) return; // valid melee attack against pawn
            var os = initiatingVerb.CasterPawn.PawnOSVerb();
            if (os is null) return; // valid melee attack against pawn by someone who can FollowUp
            #endregion Exit if situation is unrelated to OS
            os.FollowUpLogic(initiatingVerb);
        }
        
    }
    
}
