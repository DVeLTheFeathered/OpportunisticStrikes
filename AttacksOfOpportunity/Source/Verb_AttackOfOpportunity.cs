using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TestMod
{
    
    public class Verb_AttackOfOpportunity: Verb_MeleeAttack
    {
        protected const float verbChance = 1.0f;
        protected List<Verb> verbs = new List<Verb>();
        protected bool comboIsFinished;
        protected int attacksMade;
        public MeleeAttackResult pawnLastAttackResult;
        protected Stance originalStance;
        public int AttacksRemaining => comboIsFinished ? 0 : verbs.Count - attacksMade;
        protected bool InterruptingAttackResult => pawnLastAttackResult != MeleeAttackResult.Hit;

        protected override int ShotsPerBurst => verbs.Count;

        protected override bool TryCastShot()
        {
            Verb verb = verbs.ElementAtOrDefault(attacksMade);
            if (verb is null
                || (currentTarget.Pawn?.Downed ?? false)
                || !verb.IsStillUsableBy(CasterPawn))
            {
                comboIsFinished = true;
                return false;
            }
            
            bool successfulHit = verb.TryStartCastOn(
                    CurrentTarget,
                    surpriseAttack,
                    canHitNonTargetPawnsNow,
                    preventFriendlyFire,
                    NonInterruptingSelfCast) 
                && !InterruptingAttackResult;
            attacksMade++;
            comboIsFinished = !successfulHit || AttacksRemaining == 0;
            Log.Message($"Verb_AttackOfOpportunity.TryCastShot. {CasterPawn} used attack №{attacksMade}: {verb}. Result: {pawnLastAttackResult}. comboIsFinished: {comboIsFinished}");
            return successfulHit;
        }

        public void FollowUpLogic(Verb_MeleeAttack initiatingVerb)
        {
            // TODO should moving pawn counterattack?
            Log.Message($"Verb_AttackOfOpportunity.FollowUpLogic: {initiatingVerb}");
            if (comboIsFinished) { // Finalize AoO if it was the last verb of combo
                Log.Message($"Verb_AttackOfOpportunity.FollowUpLogic: Finalizing AoA");
                if (!(originalStance is null)) CasterPawn.stances.SetStance(originalStance); // cooldown of combo is cooldown of initiating attack
                Reset();
                return;
            }
            
            if (Bursting // we are already in followUp sequence
                || InterruptingAttackResult
                || (currentTarget.Pawn?.Downed ?? false)
                || (CasterPawn.MentalState?.def is MentalStateDef mentalStateDef && new []{ MentalStateDefOf.SocialFighting, CachedValues.SadisticRage }.Contains(mentalStateDef))) // less accidental kills here
                return; 
            
            TryStartFollowUp(initiatingVerb);
        }

        protected bool TryStartFollowUp(Verb_MeleeAttack initiatingVerb)
        {
            Reset();
            RerollVerbs(initiatingVerb);
            if (verbs.Count == 0) return false;
            originalStance = CasterPawn.stances.curStance;
            // Stance_Warmup can be interrupted by player, resetting cooldown.
            CasterPawn.stances.SetStance(new Stance_CooldownWithFollowUp(TicksBetweenBurstShots, initiatingVerb.CurrentTarget, this));
            Log.Message($"Verb_AttackOfOpportunity.TryStartFollowUp: {CasterPawn} {verbs.Count} {verbs[0]}");
            return true;
        }

        protected void RerollVerbs(Verb_MeleeAttack initiatingVerb)
        {
             verbs = CasterPawn.meleeVerbs.GetUpdatedAvailableVerbsList(false)
                .Select(x => x.verb)
                .Where(x=> x != initiatingVerb && Rand.Chance(verbChance * (x.tool?.chanceFactor ?? 1)))
                .OrderBy(x => Guid.NewGuid()) // randomise order
                .ToList();
        }

        public override void Reset()
        {
            base.Reset();
            verbs = new List<Verb>();
            attacksMade = 0;
            comboIsFinished = false;
            originalStance = null;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref verbs, "verbs", LookMode.Reference);
            Scribe_Values.Look(ref attacksMade, "attacksMade", 0);
            Scribe_Values.Look(ref comboIsFinished, "comboIsFinished", false);
            Scribe_Values.Look(ref pawnLastAttackResult, "pawnLastAttackResult");
            Scribe_Deep.Look(ref originalStance, "originalStance", null);
        }

        protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            throw new NotImplementedException();
        }
    }
    
}
