using UnityEngine;
using UnityEngine.Events;

namespace Frankie.Combat
{
    [RequireComponent(typeof(CombatParticipant))]
    public class CombatParticipantStateListener : MonoBehaviour
    {
        // Tunables
        [SerializeField] private StateAlteredType typeToMatch = StateAlteredType.Dead;
        [SerializeField] private UnityEvent matchInteractionEvent;

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
            matchInteractionEvent.Invoke();
        }
        #endregion
    }
}
