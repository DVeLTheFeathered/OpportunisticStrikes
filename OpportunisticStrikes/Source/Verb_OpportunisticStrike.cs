using System;
using System.Collections.Generic;
using System.Linq;
using OpportunisticStrikes.Settings;
using RimWorld;
using Verse;

namespace OpportunisticStrikes
{
    
    public class Verb_OpportunisticStrike: Verb_MeleeAttack
    {
        protected List<Verb_MeleeAttack> verbs = new List<Verb_MeleeAttack>();
        protected bool comboIsFinished;
        protected int attacksMade;
        public MeleeAttackResult pawnLastAttackResult;
        protected Stance originalStance;
        public int AttacksRemaining => comboIsFinished ? 0 : verbs.Count - attacksMade;
        
        protected override int ShotsPerBurst => verbs.Count;

        protected override bool TryCastShot()
        {
            Verb_MeleeAttack verb = verbs.ElementAtOrDefault(attacksMade);
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
                && !OSSettings.InterruptingResult(pawnLastAttackResult);
            attacksMade++;
            comboIsFinished = !successfulHit || AttacksRemaining == 0;
            Log.Message($"Verb_OpportunisticStrike.TryCastShot. {CasterPawn} used attack №{attacksMade}: {verb}. Result: {pawnLastAttackResult}. comboIsFinished: {comboIsFinished}");
            return successfulHit;
        }

        public void FollowUpLogic(Verb_MeleeAttack initiatingVerb)
        {
            // TODO do not forget to add Language to defaul rimworld project

            // TODO should moving pawn counterattack?
            Log.Message($"Verb_OpportunisticStrike.FollowUpLogic: {initiatingVerb}");
            if (comboIsFinished) { // Finalize AoO if it was the last verb of combo
                Log.Message($"Verb_OpportunisticStrike.FollowUpLogic: Finalizing AoA");
                if (!(originalStance is null)) CasterPawn.stances.SetStance(originalStance); // cooldown of combo is cooldown of initiating attack
                Reset();
                return;
            }
            
            // We should not FollowUp if
            if (Bursting // we are already in followUp sequence
                || OSSettings.InterruptingResult(pawnLastAttackResult)
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
            Log.Message($"Verb_OpportunisticStrike.TryStartFollowUp: {CasterPawn}. " +
                        $"Chance: {OSSettings.VerbChanceFromSkill(CasterPawn.skills?.GetSkill(SkillDefOf.Melee)?.GetLevel() ?? 0)}%. " +
                        $"Verbs: {verbs.Count}. " +
                        $"Initiated by {initiatingVerb}");
            return true;
        }

        protected void RerollVerbs(Verb_MeleeAttack initiatingVerb)
        {
             verbs = CasterPawn.meleeVerbs.GetUpdatedAvailableVerbsList(false)
                .Select(x => x.verb as Verb_MeleeAttack)
                .Where(x=> x != null && x != initiatingVerb && Rand.Chance(x.VerbChanceInCombo()))
                .OrderBy(x => Guid.NewGuid()) // randomise order
                .ToList();
        }

        public override void Reset()
        {
            base.Reset();
            verbs = new List<Verb_MeleeAttack>();
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
            Log.Error($"Verb_OpportunisticStrike.ApplyMeleeDamageToTarget() was called directly, which should never happen. Most probably a mod conflict.");
            throw new NotImplementedException();
        }

    }
    
}
