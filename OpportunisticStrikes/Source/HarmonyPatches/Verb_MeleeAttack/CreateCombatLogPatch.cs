using HarmonyLib;
using Verse;

namespace OpportunisticStrikes.HarmonyPatches.Verb_MeleeAttack
{
    [HarmonyPatch(typeof(RimWorld.Verb_MeleeAttack), nameof(RimWorld.Verb_MeleeAttack.CreateCombatLog))]
    public static class CreateCombatLogPatch
    {
        
        public static void Postfix(
            RimWorld.Verb_MeleeAttack __instance, 
            Verse.BattleLogEntry_MeleeCombat __result)
        {
            var aoo = __instance.CasterPawn.PawnOSVerb();
            if (aoo is null) return;
            if (aoo.Bursting) {
                __result.def = CachedValues.MeleeAttackCombo;
                __result.alwaysShowInCompact = true;
            }

            RulePackDef attackResult = __result.RuleDef;
            ManeuverDef maneuver = __instance.maneuver;
            switch (attackResult)
            {
                case var _ when ReferenceEquals(attackResult, maneuver.combatLogRulesHit):
                    aoo.pawnLastAttackResult = MeleeAttackResult.Hit;
                    break;
                case var _ when ReferenceEquals(attackResult, maneuver.combatLogRulesMiss):
                    aoo.pawnLastAttackResult = MeleeAttackResult.Miss;
                    break;
                case var _ when ReferenceEquals(attackResult, maneuver.combatLogRulesDodge):
                    aoo.pawnLastAttackResult = MeleeAttackResult.Dodge;
                    break;
                case var _ when ReferenceEquals(attackResult, maneuver.combatLogRulesDeflect):
                    aoo.pawnLastAttackResult = MeleeAttackResult.Deflect;
                    break;
                default:
                    aoo.pawnLastAttackResult = MeleeAttackResult.Undefined;
                    break;
            }
        }
    }
}
