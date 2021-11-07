using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Remove Status Effect", menuName = "BattleAction/Effects/Remove Status Effect")]
    public class RemovePersistentStatusEffect : EffectStrategy
    {
        // Tunables
        [SerializeField] bool removePersistentRecurring = true;
        [SerializeField] bool removePersistentStatModifier = true;
        [SerializeField] [Range(0, 1)] float fractionProbabilityToRemove = 1.0f;
        [Tooltip("Set to 0 to remove all")][Min(0)] [SerializeField] int numberOfEffectsToRemove = 0;
        [Tooltip("Set to none for any")] [SerializeField] StatusType statusType = StatusType.None;

        public override void StartEffect(CombatParticipant sender, IEnumerable<CombatParticipant> recipients, Action<EffectStrategy> finished)
        {
            if (!removePersistentRecurring && !removePersistentStatModifier) { return; }
            if (recipients == null) { return; }

            foreach (CombatParticipant combatParticipant in recipients)
            {
                float chanceRoll = UnityEngine.Random.Range(0f, 1f);
                if (fractionProbabilityToRemove < chanceRoll) { return; }

                PersistentStatus[] persistentStatuses = GetRelevantStatuses(combatParticipant);
                if (persistentStatuses == null || persistentStatuses.Length == 0) { return; }

                persistentStatuses.Shuffle();

                if (numberOfEffectsToRemove == 0) { numberOfEffectsToRemove = int.MaxValue; }
                int i = 0;
                foreach (PersistentStatus persistentStatus in Filter(persistentStatuses, statusType))
                {
                    if (i >= numberOfEffectsToRemove) { break; }

                    Destroy(persistentStatus);
                    i++;
                }
            }

            finished.Invoke(this);
        }

        private IEnumerable<PersistentStatus> Filter(IEnumerable<PersistentStatus> statuses, StatusType statusType)
        {
            foreach (PersistentStatus status in statuses)
            {
                if (statusType == StatusType.None)
                {
                    yield return status;
                }
                else
                {
                    if (status.GetStatusType() == statusType)
                    {
                        yield return status;
                    }
                }

            }
        }

        private PersistentStatus[] GetRelevantStatuses(CombatParticipant combatParticipant)
        {
            PersistentStatus[] persistentStatuses = null;

            if (removePersistentRecurring && removePersistentStatModifier)
            {
                persistentStatuses = combatParticipant.GetComponents<PersistentStatus>();
            }
            else if (removePersistentRecurring && !removePersistentStatModifier)
            {
                persistentStatuses = combatParticipant.GetComponents<PersistentRecurringStatus>();
            }
            else if (!removePersistentRecurring && removePersistentStatModifier)
            {
                persistentStatuses = combatParticipant.GetComponents<PersistentStatModifierStatus>();
            }

            return persistentStatuses;
        }
    }
}
