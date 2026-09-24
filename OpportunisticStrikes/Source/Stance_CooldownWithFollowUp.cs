using Verse;

namespace OpportunisticStrikes
{
    internal class Stance_CooldownWithFollowUp : Stance_Cooldown
    {
        public Stance_CooldownWithFollowUp()
        {
        }

        public Stance_CooldownWithFollowUp(int ticks, LocalTargetInfo focusTarg, Verb verb) : base(ticks, focusTarg, verb) { }
        
        protected override void Expire()
        {
            verb.TryStartCastOn(focusTarg);
            base.Expire();
        }
    }
}
