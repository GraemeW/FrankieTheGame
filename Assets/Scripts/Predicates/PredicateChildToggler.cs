using Frankie.Control;
using Frankie.Saving;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject.TryGetComponent(out PlayerStateMachine playerStateMachine))
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

