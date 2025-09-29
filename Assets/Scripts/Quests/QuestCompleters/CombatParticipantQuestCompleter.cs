using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Combat;

namespace Frankie.Quests
{
    [RequireComponent(typeof(CombatParticipant))]
    public class CombatParticipantQuestCompleter : QuestCompleter
    {
        [SerializeField] StateAlteredType typeToMatch = StateAlteredType.Dead;

        // Cached References
        CombatParticipant combatParticipant = null;

        private void Awake()
        {
            combatParticipant = GetComponent<CombatParticipant>();
        }

        private void OnEnable()
        {
            combatParticipant.SubscribeToStateUpdates(CompleteObjective);
        }

        private void OnDisable()
        {
            combatParticipant.UnsubscribeToStateUpdates(CompleteObjective);
        }

        private void CompleteObjective(StateAlteredEvent stateAlteredEvent)
        {
            if (stateAlteredEvent.stateAlteredType == typeToMatch)
            {
                CompleteObjective();
            }
        }
    }
}