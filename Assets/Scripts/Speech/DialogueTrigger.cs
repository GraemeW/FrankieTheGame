using Frankie.Control;
using Frankie.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Frankie.Speech
{
    public class DialogueTrigger : MonoBehaviour
    {
        // Tunables
        [SerializeField] DialogueUpdateType dialogueUpdateType = DialogueUpdateType.DialogueComplete;
        [SerializeField] DialogueNode triggerNode;
        [SerializeField] UnityEventWithCallingController onTriggerWithCallingController;

        // Cached References
        DialogueController dialogueController = null;
        PlayerStateHandler playerStateHandler = null;

        // Data Structures
        [System.Serializable]
        public class UnityEventWithCallingController : UnityEvent<PlayerStateHandler>
        {
        }

        public void Setup(DialogueController dialogueController, PlayerStateHandler playerStateHandler)
        {
            this.playerStateHandler = playerStateHandler;
            this.dialogueController = dialogueController;
            dialogueController.dialogueUpdated += Trigger;
        }

        private void Trigger(DialogueUpdateType dialogueUpdateType, DialogueNode dialogueNode)
        {
            if (triggerNode == null) { return; }
            if (this.dialogueUpdateType != dialogueUpdateType) { return; }

            // Node entry & exit conditions
            if (dialogueUpdateType == DialogueUpdateType.DialogueNodeEntry || dialogueUpdateType == DialogueUpdateType.DialogueNodeExit
                && triggerNode == dialogueNode)
            {
                if (onTriggerWithCallingController != null)
                {
                    onTriggerWithCallingController.Invoke(playerStateHandler);
                }
            }
            // Dialogue main state conditions
            else if (dialogueUpdateType == DialogueUpdateType.DialogueInitiated || dialogueUpdateType == DialogueUpdateType.DialogueComplete)
            {
                if (onTriggerWithCallingController != null)
                {
                    onTriggerWithCallingController.Invoke(playerStateHandler);
                }
            }

            // Unsubscribe
            if (dialogueUpdateType == DialogueUpdateType.DialogueComplete)
            {
                dialogueController.dialogueUpdated -= Trigger;
            }
        }
    }
}