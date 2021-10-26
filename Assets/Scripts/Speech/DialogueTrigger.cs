using Frankie.Control;
using Frankie.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Frankie.Speech
{
    public class DialogueTrigger : MonoBehaviour
    {
        // Tunables
        [SerializeField] DialogueTriggerType dialogueTriggerType = DialogueTriggerType.onDialogueComplete;
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
            dialogueController.dialogueNodeEntered += TriggerOnEntry;
            dialogueController.dialogueNodeExited += TriggerOnExit;
            dialogueController.dialogueComplete += TriggerOnComplete;
        }

        private void OnDestroy()
        {
            if (dialogueController != null)
            {
                dialogueController.dialogueNodeEntered -= TriggerOnEntry;
                dialogueController.dialogueNodeExited -= TriggerOnExit;
                dialogueController.dialogueComplete -= TriggerOnComplete;
            }
        }

        private void TriggerOnEntry(DialogueNode dialogueNode)
        {
            if (triggerNode == null) { return; }
            if (dialogueTriggerType != DialogueTriggerType.onDialogueNodeEntry) { return; }

            Trigger();
        }

        private void TriggerOnExit(DialogueNode dialogueNode)
        {
            if (triggerNode == null) { return; }
            if (dialogueTriggerType != DialogueTriggerType.onDialogueNodeExit) { return; }

            Trigger();
        }

        private void TriggerOnComplete()
        {
            if (triggerNode == null) { return; }
            if (dialogueTriggerType != DialogueTriggerType.onDialogueComplete) { return; }

            Trigger();
        }

        private void Trigger()
        {
            if (onTriggerWithCallingController != null)
            {
                onTriggerWithCallingController.Invoke(playerStateHandler);
            }
        }
    }
}