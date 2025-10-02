using UnityEngine;
using Frankie.Control;
using Frankie.Saving;

namespace Frankie.Core
{
    public class PredicateChildToggler : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] Condition condition = null;

        // State
        bool childrenEnabled = true;

        // Unity Methods
        void OnEnable()
        {
            if (condition == null) { return; }
            ToggleChildrenOnCondition();
        }

        // Public Methods
        public void ToggleChildrenOnCondition() // Callable via Unity Events
        {
            PlayerStateMachine playerStateMachine = Player.FindPlayerStateMachine();
            if (playerStateMachine != null)
            {
                if (condition.Check(playerStateMachine.GetComponents<IPredicateEvaluator>()))
                {
                    foreach (Transform child in transform)
                    {
                        child.gameObject.SetActive(true);
                    }
                    childrenEnabled = true;
                }
                else
                {
                    foreach (Transform child in transform)
                    {
                        child.gameObject.SetActive(false);
                    }
                    childrenEnabled = false;
                }
            }
        }

        // Interface
        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        public SaveState CaptureState()
        {
            return new SaveState(GetLoadPriority(), childrenEnabled);
        }

        public void RestoreState(SaveState saveState)
        {
            childrenEnabled = (bool)saveState.state;
            foreach (Transform child in transform) { child.gameObject.SetActive(childrenEnabled); }
        }
    }
}
