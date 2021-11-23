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
            combatParticipant.stateAltered += CompleteObjective;
        }

        private void OnDisable()
        {
            combatParticipant.stateAltered -= CompleteObjective;
        }

        private void CompleteObjective(CombatParticipant combatParticipant, StateAlteredData stateAlteredData)
        {
            if (stateAlteredData.stateAlteredType == typeToMatch)
            {
                CompleteObjective();
            }
        }
    }
}