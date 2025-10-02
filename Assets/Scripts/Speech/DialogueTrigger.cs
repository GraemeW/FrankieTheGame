using UnityEngine;
using UnityEngine.Events;
using Frankie.Control;

namespace Frankie.Speech
{
    public class DialogueTrigger : MonoBehaviour
    {
        // Tunables
        [SerializeField] DialogueUpdateType dialogueUpdateType = DialogueUpdateType.DialogueComplete;
        [SerializeField] DialogueNode triggerNode;
        [SerializeField][Tooltip("Trigger immediate for node-based entry/exit not requiring player state change")] DialogueTriggeredEvent dialogueTriggeredEvent;

        // State
        bool queuedTrigger = false;

        // Cached References
        DialogueController dialogueController = null;
        PlayerStateMachine playerStateHandler = null;

        // Data Structures
        [System.Serializable]
        public struct DialogueTriggeredEvent
        {
            public bool triggerImmediate;
            public UnityEventWithCallingController onTriggerEvent;
        }

        [System.Serializable]
        public class UnityEventWithCallingController : UnityEvent<PlayerStateMachine>
        {
        }

        public void Setup(DialogueController dialogueController, PlayerStateMachine playerStateHandler)
        {
            this.playerStateHandler = playerStateHandler;
            this.dialogueController = dialogueController;
            queuedTrigger = false; // Reset state on new conversation (avoids triggering on unrelated nodes on subsequent conversations)
            dialogueController.dialogueUpdated += Trigger;
        }

        private void Trigger(DialogueUpdateType dialogueUpdateType, DialogueNode dialogueNode)
        {
            if (dialogueUpdateType == DialogueUpdateType.DialogueComplete)
            {
                dialogueController.dialogueUpdated -= Trigger;
                if (queuedTrigger) { dialogueTriggeredEvent.onTriggerEvent?.Invoke(playerStateHandler); }
            }

            if (this.dialogueUpdateType != dialogueUpdateType) { return; }
            if (HandleNodeEntryExit(dialogueUpdateType, dialogueNode)) { return; }
            if (HandleDialogueState(dialogueUpdateType)) { return; }
        }

        private bool HandleNodeEntryExit(DialogueUpdateType dialogueUpdateType, DialogueNode dialogueNode)
        {
            if ((dialogueUpdateType == DialogueUpdateType.DialogueNodeEntry || dialogueUpdateType == DialogueUpdateType.DialogueNodeExit)
                            && (triggerNode != null && triggerNode == dialogueNode))
            {
                if (dialogueTriggeredEvent.triggerImmediate)
                {
                    dialogueTriggeredEvent.onTriggerEvent?.Invoke(playerStateHandler);
                }
                else
                {
                    queuedTrigger = true;
                }
                return true;
            }
            return false;
        }

        private bool HandleDialogueState(DialogueUpdateType dialogueUpdateType)
        {
            if (dialogueUpdateType == DialogueUpdateType.DialogueInitiated || dialogueUpdateType == DialogueUpdateType.DialogueComplete)
            {
                dialogueTriggeredEvent.onTriggerEvent?.Invoke(playerStateHandler);
                return true;
            }

            return false;
        }
    }
}
