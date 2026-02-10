using UnityEngine;
using Frankie.Control;
using Frankie.Saving;

namespace Frankie.Core
{
    public class PredicateChildToggler : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] private Condition condition;

        // State
        private bool childrenEnabled = true;

        #region UnityMethods
        private void OnEnable()
        {
            if (condition == null) { return; }
            ToggleChildrenOnCondition();
        }
        #endregion

        #region PublicMethods
        public void ToggleChildrenOnCondition() // Callable via Unity Events
        {
            PlayerStateMachine playerStateMachine = Player.FindPlayerStateMachine();
            if (playerStateMachine == null) { return; }
            
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
        #endregion

        #region SaveInterface
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;

        public SaveState CaptureState()
        {
            return new SaveState(GetLoadPriority(), childrenEnabled);
        }

        public void RestoreState(SaveState saveState)
        {
            childrenEnabled = (bool)saveState.state;
            foreach (Transform child in transform) { child.gameObject.SetActive(childrenEnabled); }
        }
        #endregion
    }
}
