using UnityEngine;
using Frankie.Saving;

namespace Frankie.Control
{
    public class PlayerColliderTrigger : MonoBehaviour, ISaveable<bool>
    {
        // Tunables
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private bool disableAfterTrigger = true;
        [SerializeField] private InteractionEvent triggerEvent;

        // State
        private bool triggered = false;

        private void OnEnable()
        {
            ReconcileState();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            GameObject collisionObject = collision.gameObject;
            if (playerLayer != (playerLayer | (1 << collisionObject.layer))) return;
            
            var playerStateMachine = collisionObject.GetComponentInParent<PlayerStateMachine>();
            if (playerStateMachine == null) { triggered = true; return; }

            triggerEvent.Invoke(playerStateMachine);
            triggered = true;
            ReconcileState();
        }

        private void ReconcileState()
        {
            if (triggered && disableAfterTrigger) { gameObject.SetActive(false); }
        }

        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;

        public SaveState CaptureState() => ManualGetStateFromData(triggered);

        public void RestoreState(SaveState saveState)
        {
            triggered = ManualGetDataFromState(saveState);
            ReconcileState();
        }

        public SaveState ManualGetStateFromData(bool data) => new(GetLoadPriority(), data);

        public bool ManualGetDataFromState(SaveState saveState)
        {
            if (saveState == null) { return triggered; }
            return (bool)saveState.GetState(typeof(bool));
        }
    }
}
