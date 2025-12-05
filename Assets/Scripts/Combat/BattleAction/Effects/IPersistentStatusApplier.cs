using UnityEngine;
using Frankie.Stats;

namespace Frankie.Combat
{
    public interface IPersistentStatusApplier
    {
        public bool CheckForEffect(CombatParticipant recipient);
        public PersistentStatus Apply(CombatParticipant sender, CombatParticipant recipient, DamageType damageType);

        public static bool CheckProbabilityToApply(float fractionProbabilityToApply)
        {
            if (Mathf.Approximately(fractionProbabilityToApply, 0f)) { return false; }
            if (Mathf.Approximately(fractionProbabilityToApply, 1f)) { return true; }
            
            float chanceRoll = UnityEngine.Random.Range(0f, 1f);
            return chanceRoll < fractionProbabilityToApply;
        }

        public static bool CheckProbabilityToApply(CombatParticipant sender, CombatParticipant recipient, Stat statForContest)
        {
            if (sender == null || recipient == null) { return false; }
            
            float attackerModifier = sender.GetStat(statForContest);
            float defenderModifier = recipient.GetStat(statForContest);
            float fractionProbabilityToApply = CalculatedStats.GetContestedStatProbability(attackerModifier, defenderModifier);
            
            return CheckProbabilityToApply(fractionProbabilityToApply);
        }
    }
}
