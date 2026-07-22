using HarmonyLib;
using Verse.Grammar;

namespace TestMod.HarmonyPatches.BattleLogEntry_MeleeCombat
{
    [HarmonyPatch(typeof(Verse.BattleLogEntry_MeleeCombat), "GenerateGrammarRequest")]
    public static class GenerateGrammarRequestPatch
    {
        
        public static void Postfix(Verse.BattleLogEntry_MeleeCombat __instance, GrammarRequest __result)
        {
            if (__instance.def == CachedValues.MeleeAttackCombo)
                __result.Rules.Add(CachedValues.AoORule);
        }

    }
}
