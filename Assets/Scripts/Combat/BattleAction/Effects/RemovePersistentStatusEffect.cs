using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Remove Status Effect", menuName = "BattleAction/Effects/Remove Status Effect")]
    public class RemovePersistentStatusEffect : EffectStrategy
    {
        // Tunables
        [SerializeField] private bool removePersistentRecurring = true;
        [SerializeField] private bool removePersistentStatModifier = true;
        [SerializeField] [Range(0, 1)] private float fractionProbabilityToRemove = 1.0f;
        [Tooltip("Set to 0 to remove all")][Min(0)] [SerializeField] int numberOfEffectsToRemove;

        public override IEnumerator StartEffect(CombatParticipant sender, IList<BattleEntity> recipients, DamageType damageType)
        {
            if (!removePersistentRecurring && !removePersistentStatModifier) { yield break; }
            if (recipients == null) { yield break; }

            foreach (BattleEntity recipient in recipients)
            {
                float chanceRoll = UnityEngine.Random.Range(0f, 1f);
                if (fractionProbabilityToRemove < chanceRoll) { continue; }

                var persistentStatuses = GetRelevantStatuses(recipient.combatParticipant);
                if (persistentStatuses.Count == 0) { continue; }
                persistentStatuses.Shuffle();

                if (numberOfEffectsToRemove == 0) { numberOfEffectsToRemove = int.MaxValue; }
                int i = 0;
                foreach (PersistentStatus persistentStatus in persistentStatuses)
                {
                    if (i >= numberOfEffectsToRemove) { break; }

                    Destroy(persistentStatus);
                    i++;
                }
            }
        }

        private IList<PersistentStatus> GetRelevantStatuses(CombatParticipant combatParticipant)
        {
            var persistentStatuses = new List<PersistentStatus>();

            if (removePersistentRecurring && removePersistentStatModifier)
            {
                persistentStatuses.AddRange(combatParticipant.GetComponents<PersistentStatus>());
            }
            else if (removePersistentRecurring && !removePersistentStatModifier)
            {
                persistentStatuses.AddRange(combatParticipant.GetComponents<PersistentRecurringStatus>());
            }
            else if (!removePersistentRecurring && removePersistentStatModifier)
            {
                persistentStatuses.AddRange(combatParticipant.GetComponents<PersistentStatModifierStatus>());
            }
            return persistentStatuses;
        }
    }
}
