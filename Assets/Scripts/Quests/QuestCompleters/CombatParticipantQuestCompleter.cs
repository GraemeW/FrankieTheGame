using UnityEngine;
using Frankie.Combat;

namespace Frankie.Quests
{
    [RequireComponent(typeof(CombatParticipant))]
    public class CombatParticipantQuestCompleter : QuestCompleter
    {
        // Tunables
        [SerializeField] private StateAlteredType typeToMatch = StateAlteredType.Dead;

        // Cached References
        private CombatParticipant combatParticipant;

        #region UnityMethods
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
        #endregion

        #region PrivateMethods
        private void CompleteObjective(StateAlteredInfo stateAlteredInfo)
        {
            if (stateAlteredInfo.stateAlteredType != typeToMatch) { return; }
            CompleteObjective();
        }
        #endregion
    }
}
