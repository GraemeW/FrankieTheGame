using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New DoT Effect", menuName = "BattleAction/Effects/DoT Effect")]
    public class HealthOverTimeEffect : EffectStrategy
    {
        [SerializeField] [Range(0, 1)] float fractionProbabilityToApply = 0.5f;
        [SerializeField] StatusType statusEffectType = default;
        [SerializeField] float duration = 10f;
        [SerializeField] float tickPeriod = 3f;
        [SerializeField] float healthChangePerTick = -10f;
        [SerializeField] bool persistAfterCombat = false;

        public override void StartEffect(CombatParticipant sender, IEnumerable<CombatParticipant> recipients, Action<EffectStrategy> finished)
        {
            if (recipients == null) { return; }

            foreach (CombatParticipant combatParticipant in recipients)
            {
                float chanceRoll = UnityEngine.Random.Range(0f, 1f);
                if (fractionProbabilityToApply < chanceRoll) { return; }

                PersistentRecurringStatus activeStatusEffect = combatParticipant.gameObject.AddComponent(typeof(PersistentRecurringStatus)) as PersistentRecurringStatus;
                activeStatusEffect.Setup(statusEffectType, duration, tickPeriod, () => combatParticipant.AdjustHP(healthChangePerTick), persistAfterCombat);

                combatParticipant.AnnounceStateUpdate(new StateAlteredData(StateAlteredType.StatusEffectApplied, statusEffectType));
            }

            finished?.Invoke(this);
        }
    }
}