using Frankie.Control;
using Frankie.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Frankie.Speech
{
    public class DialogueTrigger : MonoBehaviour
    {
        // Tunables
        [SerializeField] DialogueUpdateType dialogueUpdateType = DialogueUpdateType.DialogueComplete;
        [SerializeField] DialogueNode triggerNode;
        [SerializeField][Tooltip("Use for anything not requiring player state change")] UnityEventWithCallingController onTriggerImmediate;
        [SerializeField] [Tooltip("Use for anything that force player state change")] UnityEventWithCallingController onTriggerConversationComplete;

        // State
        bool queuedTrigger = false;

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
            queuedTrigger = false; // Reset state on new conversation (avoids triggering on unrelated nodes on subsequent conversations)
            dialogueController.dialogueUpdated += Trigger;
        }

        private void Trigger(DialogueUpdateType dialogueUpdateType, DialogueNode dialogueNode)
        {
            if (dialogueUpdateType == DialogueUpdateType.DialogueComplete)
            {
                dialogueController.dialogueUpdated -= Trigger;
                if (queuedTrigger) { onTriggerConversationComplete?.Invoke(playerStateHandler); }
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
                onTriggerImmediate?.Invoke(playerStateHandler);
                queuedTrigger = true;
                return true;
            }
            return false;
        }

        private bool HandleDialogueState(DialogueUpdateType dialogueUpdateType)
        {
            if (dialogueUpdateType == DialogueUpdateType.DialogueInitiated || dialogueUpdateType == DialogueUpdateType.DialogueComplete)
            {
                onTriggerImmediate?.Invoke(playerStateHandler);
                return true;
            }

            return false;
        }
    }
}