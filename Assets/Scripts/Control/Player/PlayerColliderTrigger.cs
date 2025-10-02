using UnityEngine;
using Frankie.Saving;

namespace Frankie.Control
{
    public class PlayerColliderTrigger : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] LayerMask playerLayer = new LayerMask();
        [SerializeField] bool disableAfterTrigger = true;
        [SerializeField] InteractionEvent triggerEvent;

        // State
        bool triggered = false;

        private void OnEnable()
        {
            ReconcileState();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            GameObject collisionObject = collision.gameObject;
            if (playerLayer == (playerLayer | (1 << collisionObject.layer)))
            {
                PlayerStateMachine playerStateMachine = collisionObject.GetComponentInParent<PlayerStateMachine>();
                if (playerStateMachine == null) { triggered = true; return; }

                triggerEvent.Invoke(playerStateMachine);
                triggered = true;
                ReconcileState();
            }
        }

        private void ReconcileState()
        {
            if (triggered && disableAfterTrigger) { gameObject.SetActive(false); }
        }

        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        public SaveState CaptureState()
        {
            return new SaveState(GetLoadPriority(), triggered);
        }

        public void RestoreState(SaveState saveState)
        {
            triggered = (bool)saveState.state;
            ReconcileState();
        }
    }
}
