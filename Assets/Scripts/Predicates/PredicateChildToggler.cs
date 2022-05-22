using Frankie.Control;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    public class PredicateChildToggler : MonoBehaviour
    {
        [SerializeField] Condition condition = null;

        void OnEnable()
        {
            if (condition == null) { return; }
            ToggleChildrenOnCondition();
        }

        private void ToggleChildrenOnCondition()
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
                }
                else
                {
                    foreach (Transform child in transform)
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}

