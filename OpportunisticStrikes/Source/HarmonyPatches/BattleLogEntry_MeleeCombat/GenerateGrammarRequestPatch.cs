using HarmonyLib;
using OpportunisticStrikes.Settings;
using Verse.Grammar;

namespace OpportunisticStrikes.HarmonyPatches.BattleLogEntry_MeleeCombat
{
    [HarmonyPatch(typeof(Verse.BattleLogEntry_MeleeCombat), "GenerateGrammarRequest")]
    public static class GenerateGrammarRequestPatch
    {
        
        public static void Postfix(Verse.BattleLogEntry_MeleeCombat __instance, GrammarRequest __result)
        {
            if (__instance.def == CachedValues.MeleeAttackCombo && OSSettings.MarkOSVerbsInCombatLog)
                __result.Rules.Add(CachedValues.OSRule);
        }

    }
}
