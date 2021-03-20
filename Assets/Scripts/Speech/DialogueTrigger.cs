using Frankie.Control;
using Frankie.Core;
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
        public class UnityEventWithCallingController : UnityEvent<PlayerStateHandler>
        {
        }

        public void Trigger(string actionToTrigger, PlayerStateHandler playerStateHandler)
        {
            if (actionToTrigger == action)
            {
                onTriggerWithCallingController.Invoke(playerStateHandler);
            }
        }
    }
}