using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine.Rendering;
using Verse;

namespace TestMod.HarmonyPatches.Verb_MeleeAttack
{
    [HarmonyPatch(typeof(RimWorld.Verb_MeleeAttack), "TryCastShot")]
    public static class TryCastShotPatch
    {
        /// <summary>
        /// Why Verb_MeleeAttack can't burst, but Verb_LaunchProjectile can?
        /// Because in Verb_MeleeAttack.TryCastShot() we have a check:
        /// if (!casterPawn.Spawned || casterPawn.stances.FullBodyBusy)
        ///    return false;
        /// Where FullBodyBusy checks if pawn is stunned or is otherwise occupied. And "Bursting" is considered occupied, thus interrupting burst.
        /// Then why does Verb_LaunchProjectile does not need this check? Does it just ignore stuns and being spawned?
        /// I found that Verb.VerbTick() has a similar check for bursting cases:
        /// if (this.state == VerbState.Bursting)
        ///    {
        ///      if (!this.caster.Spawned || this.caster is Pawn caster && caster.stances.stunner.Stunned)
        ///      {
        ///        this.Reset();
        ///      }
        ///     .......
        /// Difference being that instead of FullBodyBusy ("stunned" or "busy") it just checks for "stunned".
        /// It means that in case of Verb_MeleeAttack we can safely skip it's check (since it was already done in VerbTick) as long as we are VerbState.Bursting
        /// 
        /// To increase chances of future compatibility, I search for the check starting with "casterPawn.Spawned" and up to 
        /// "Thing thing = this.currentTarget.Thing"
        /// which is the next line after this check.
        /// </summary>
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> BurstingMeleeVerbsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            var finalInstructions = new List<CodeInstruction>(instructions);

            int firstGetSpawned = -1;
            int firstGetThing = -1;
            bool patched = false;
            for (int i = 0; i < finalInstructions.Count; i++) {
                if (finalInstructions[i].opcode != OpCodes.Callvirt && finalInstructions[i].opcode != OpCodes.Call) continue;
                string operandStr = finalInstructions[i].operand.ToString();
                if (firstGetSpawned < 0 && operandStr.Contains("get_Spawned")) {
                    firstGetSpawned = i;
                }
                if (firstGetThing < 0 && operandStr.Contains("get_Thing")) {
                    firstGetThing = i;
                }

                if (firstGetSpawned < 0 || firstGetThing < 0) continue;

                //IL_0020: ldarg.0      // this                                                 <-- To this (firstGetThing - 2)
                //IL_0021: ldflda valuetype Verse.LocalTargetInfo Verse.Verb::currentTarget
                //IL_0026: call instance class Verse.Thing Verse.LocalTargetInfo::get_Thing()
                //IL_002b: stloc.2      // thing
                Label afterCheck = ilGenerator.DefineLabel();
                finalInstructions[firstGetThing - 2].labels.Add(afterCheck);

                List<CodeInstruction> skipIfBursting = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0), // this
                    new CodeInstruction(OpCodes.Ldfld, typeof(Verse.Verb).GetField("state")), 
                    new CodeInstruction(OpCodes.Ldc_I4_1),  //VerbState.Bursting
                    new CodeInstruction(OpCodes.Beq_S, afterCheck),
                };

                //IL_0007: ldloc.1      // casterPawn                                           <-- Before this (firstGetSpawned)
                //IL_0008: callvirt instance bool Verse.Thing::get_Spawned()
                //IL_000d: brtrue.s IL_0011                                                     
                finalInstructions.InsertRange(firstGetSpawned - 1, skipIfBursting);

                patched = true;
                break;
            }

            if (!patched) Log.Error($"Attack Of Opportunity: Failed to patch {nameof(RimWorld.Verb_MeleeAttack)}.TryCastShot with {nameof(BurstingMeleeVerbsTranspiler)}. Most probable reason - Rimworld update or another mod messing with this method");
            return finalInstructions;
        }

        /// <summary>
        /// Why =(
        /// CreateCombatLog() is called to register Hit, Dodge and Miss.
        /// But when we need to register Deflection, we first create "Hit" log, THEN check for deflection.
        /// ...Aaaaaaand then we do checks if log was needed in the first place, and then we either 
        ///     search log history and DELETE it, 
        ///     or MANUALLY REPLACE "Hit" log parameters to "Deflect" parameters
        /// It pains me. There is no "Deflection" log. Only "Hit" log that is not "Hit" anymore.
        /// 
        /// I need to differentiate types of hits, and TryCastShot does not differentiate Miss/Dodge and Hit/Deflect.
        /// And TryCastBurstShot does not differentiate at all. 
        /// CreateCombatLog() was a reasonable solution, but now this...
        /// I try to be as careful as I can, and replacing part of original code is not "careful". But let's hope for the best.
        /// 
        /// I will remove two parts of code: 
        /// 1) Code1 - unnecessary creation of combat log.
        ///     FleckMaker.Static(drawPos, map, this.verbProps.impactFleck);
        ///      // here
        ///     flag = true;
        /// 2) Code2 - handling of Deflection in weird way
        ///     flag = true;
        ///      // here
        ///     combatLog.alwaysShowInCompact = false; 
        ///     including last line
        /// Then I will add call to refactor function after 
        ///     flag = true;
        ///     // here
        /// 
        /// It would be easier to use things like alwaysShowInCompact as markers, but I believe broader approach would be more resilient to possible code changes
        /// </summary>
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> RefactoringDeflectionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            var finalInstructions = new List<CodeInstruction>(instructions);

            int firstFleckMaker = -1; // last good line before code1, remove code after this
            int firstImpactFleck = -1; // jumps into bad code, have to change
            int firstFlagTrue = -1; // good line between code1 and code2, also firstImpactFleck must jump to this
            int firstFlagFalse = -1; // first good line of code after code2. Before this, there is jump to another if branch. Must keep it
            bool patched = false;
            for (int i = 0; i < finalInstructions.Count; i++)
            {
                var instruction = finalInstructions[i];
                if (firstFleckMaker < 0 
                    && instruction.opcode == OpCodes.Call 
                    && ((instruction.operand as MethodInfo)?.DeclaringType?.ToString().Contains("FleckMaker") ?? false)
                    ) 
                    firstFleckMaker = i;

                if (firstImpactFleck < 0
                    && instruction.opcode == OpCodes.Ldfld
                    && instruction.operand.ToString().Contains("impactFleck")
                    ) 
                    firstImpactFleck = i;

                if (firstFlagTrue < 0
                    && instruction.opcode == OpCodes.Stloc_0
                    && finalInstructions[i - 1].opcode == OpCodes.Ldc_I4_1
                    ) 
                    firstFlagTrue = i;

                if (firstFlagFalse < 0
                    && instruction.opcode == OpCodes.Stloc_0
                    && finalInstructions[i - 1].opcode == OpCodes.Ldc_I4_0
                    ) 
                    firstFlagFalse = i;

                if (firstFleckMaker < 0
                    || firstImpactFleck < 0
                    || firstFlagTrue < 0
                    || firstFlagFalse < 0
                    ) continue;

                // working from end because it messes with indexes
                
                #region Redirecting labels from bad code
                //IL_01c5: ldfld        class Verse.VerbProperties Verse.Verb::verbProps
                //IL_01ca: ldfld        class Verse.FleckDef Verse.VerbProperties::impactFleck
                //IL_01cf: brfalse.s    IL_01ea                                         <-- this (firstImpactFleck + 1) must jump to ...
                // bad code
                //IL_0212: ldc.i4.1                                                     <-- ... to this (firstFlagTrue - 1)
                //IL_0213: stloc.0 //flag
                Label toFlagTrue = ilGenerator.DefineLabel();
                finalInstructions[firstFlagTrue - 1].labels.Add(toFlagTrue);
                finalInstructions[firstImpactFleck + 1] = new CodeInstruction(OpCodes.Brfalse_S, toFlagTrue);
                #endregion Redirecting labels from bad code

                #region Removing code2
                // IL_0212: ldc.i4.1
                // IL_0213: stloc.0      // flag
                // IL_0214: ldarg.0      // this                                                        <-- delete from here (firstFlagTrue + 1) ...
                int from = firstFlagTrue + 1;
                // IL_0293: stfld        bool Verse.BattleLogEntry_MeleeCombat::alwaysShowInCompact     <-- ... to here (firstFlagFalse - 3);
                // IL_0298: br           IL_031f                                                        <-- this is link to next branch, so we leave it
                // IL_029d: ldc.i4.0
                // IL_029e: stloc.0      // flag
                int to = firstFlagFalse - 3;
                finalInstructions.RemoveRange(from, to - from + 1);
                #endregion Removing code2

                #region Inserting call for refactored function
                List<CodeInstruction> callDeflectionRefactor = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0), // Verb_MeleeAttack this
                    new CodeInstruction(OpCodes.Ldloc_3), // Pawn currentTarget
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TryCastShotPatch), nameof(DeflectionRefactor)))
                };
                // IL_0212: ldc.i4.1
                // IL_0213: stloc.0      // flag
                // ... insert here (firstFlagTrue + 1)
                finalInstructions.InsertRange(firstFlagTrue + 1, callDeflectionRefactor);
                #endregion Inserting call for refactored function

                #region Removing code1
                // IL_01e5: call   void RimWorld.FleckMaker::Static(valuetype [UnityEngine.CoreModule]UnityEngine.Vector3, class Verse.Map, class Verse.FleckDef, float32)
                // IL_01ea: ldarg.0      // this                            <-- removing from here (firstFleckMaker + 1) ...
                from = firstFleckMaker + 1;
                // IL_0210: stloc.s      combatLog                          <-- ... to here (firstFlagTrue - 2)
                // IL_0212: ldc.i4.1
                // IL_0213: stloc.0      // flag
                to = firstFlagTrue - 2; 
                finalInstructions.RemoveRange(from, to - from + 1);
                #endregion Removing code1    

                patched = true;
                break;
            }
            if (!patched) Log.Error($"Attack Of Opportunity: Failed to patch {nameof(RimWorld.Verb_MeleeAttack)}.TryCastShot with {nameof(RefactoringDeflectionTranspiler)}. Most probable reason - Rimworld update or another mod messing with this method");
            return finalInstructions;
        }

        public static void DeflectionRefactor(this RimWorld.Verb_MeleeAttack verb, Pawn currentTarget)
        {
            MethodInfo applyMeleeDamageToTarget = AccessTools.Method(typeof(RimWorld.Verb_MeleeAttack), "ApplyMeleeDamageToTarget");
            DamageWorker.DamageResult target = (DamageWorker.DamageResult)applyMeleeDamageToTarget.Invoke(verb, new object[] { verb.CurrentTarget });
            if (currentTarget != null && (double)target.totalDamageDealt > 0.0) {
                MethodInfo applyMeleeSlaveSuppression = AccessTools.Method(typeof(RimWorld.Verb_MeleeAttack), "ApplyMeleeSlaveSuppression");
                applyMeleeSlaveSuppression.Invoke(verb, new object[] { currentTarget, target.totalDamageDealt });
            }
            if (target.stunned && target.parts.NullOrEmpty<BodyPartRecord>()) return;
            Verse.BattleLogEntry_MeleeCombat combatLog = target.deflected 
                ? verb.CreateCombatLog(maneuver => maneuver.combatLogRulesDeflect, false)
                : verb.CreateCombatLog(maneuver => maneuver.combatLogRulesHit, true);
            target.AssociateWithLog(combatLog);
        }

    }
}
