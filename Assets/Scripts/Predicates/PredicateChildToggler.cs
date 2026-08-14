using UnityEngine;
using LowDefMustard.Saving;
using LowDefMustard.Utils;

namespace Frankie.Core.Predicates
{
    public class PredicateChildToggler : MonoBehaviour, ISaveable<bool>
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

        public SaveState CaptureState() => ManualGetStateFromData(childrenEnabled);

        public void RestoreState(SaveState saveState)
        {
            TryManualGetDataFromState(saveState, out childrenEnabled);
            foreach (Transform child in transform) { child.gameObject.SetActive(childrenEnabled); }
        }
        
        public SaveState ManualGetStateFromData(bool data) => new(GetLoadPriority(), data);
        
        public bool TryManualGetDataFromState(SaveState saveState, out bool value)
        {
            if (saveState != null && saveState.TryGetState(out value)) { return true; }
            value = childrenEnabled;
            return true;
        }
        #endregion
    }
}
