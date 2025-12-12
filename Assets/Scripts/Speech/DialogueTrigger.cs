using UnityEngine;
using UnityEngine.Events;
using Frankie.Control;

namespace Frankie.Speech
{
    public class DialogueTrigger : MonoBehaviour
    {
        // Tunables
        [SerializeField] private DialogueUpdateType dialogueUpdateType = DialogueUpdateType.DialogueComplete;
        [SerializeField] private DialogueNode triggerNode;
        [SerializeField][Tooltip("Trigger immediate for node-based entry/exit not requiring player state change")] private DialogueTriggeredEvent dialogueTriggeredEvent;

        // State
        private bool queuedTrigger = false;

        // Cached References
        private DialogueController dialogueController;
        private PlayerStateMachine playerStateHandler;

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

        public void Setup(DialogueController setDialogueController, PlayerStateMachine setPlayerStateHandler)
        {
            playerStateHandler = setPlayerStateHandler;
            dialogueController = setDialogueController;
            queuedTrigger = false; // Reset state on new conversation (avoids triggering on unrelated nodes on subsequent conversations)
            setDialogueController.dialogueUpdated += Trigger;
        }

        private void Trigger(DialogueUpdateType triggerDialogueUpdateType, DialogueNode dialogueNode)
        {
            if (triggerDialogueUpdateType == DialogueUpdateType.DialogueComplete)
            {
                dialogueController.dialogueUpdated -= Trigger;
                if (queuedTrigger) { dialogueTriggeredEvent.onTriggerEvent?.Invoke(playerStateHandler); }
            }

            if (dialogueUpdateType != triggerDialogueUpdateType) { return; }
            if (HandleNodeEntryExit(triggerDialogueUpdateType, dialogueNode)) { return; }
            if (HandleDialogueState(triggerDialogueUpdateType)) { return; }
        }

        private bool HandleNodeEntryExit(DialogueUpdateType checkDialogueUpdateType, DialogueNode dialogueNode)
        {
            if (checkDialogueUpdateType is not (DialogueUpdateType.DialogueNodeEntry or DialogueUpdateType.DialogueNodeExit) || (triggerNode == null || triggerNode != dialogueNode))  { return false; }
            
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

        private bool HandleDialogueState(DialogueUpdateType checkDialogueUpdateType)
        {
            if (checkDialogueUpdateType is not (DialogueUpdateType.DialogueInitiated or DialogueUpdateType.DialogueComplete)) { return false; }
            
            dialogueTriggeredEvent.onTriggerEvent?.Invoke(playerStateHandler);
            return true;
        }
    }
}
