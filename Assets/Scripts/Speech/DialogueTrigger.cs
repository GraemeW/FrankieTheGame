using Frankie.Control;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Frankie.Speech
{
    public class DialogueTrigger : MonoBehaviour
    {
        [SerializeField] string action;
        [SerializeField] UnityEventWithCallingController onTriggerWithCallingController;

        // Data Structures
        [System.Serializable]
        public class UnityEventWithCallingController : UnityEvent<PlayerController>
        {
        }

        public void Trigger(string actionToTrigger, PlayerController playerController)
        {
            if (actionToTrigger == action)
            {
                onTriggerWithCallingController.Invoke(playerController);
            }
        }
    }
}