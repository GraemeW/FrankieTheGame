using Frankie.Speech;

namespace Frankie.Core.PlayerStateMemory
{
    public class DialogueMemory
    {
        public DialogueData dialogueData;

        public bool InitiateDialogue(DialogueController dialogueController)
        {
            if (dialogueController == null || dialogueData == null) { return false; }
            return dialogueData.dialogueDataType switch
            {
                DialogueDataType.StandardDialogue => dialogueController.InitiateConversation(dialogueData.aiConversant, dialogueData.dialogue),
                DialogueDataType.SimpleText => dialogueController.InitiateSimpleMessage(dialogueData.message),
                DialogueDataType.SimpleChoice => dialogueController.InitiateSimpleOption(dialogueData.message, dialogueData.choiceActionPairs),
                _ => false
            };
        }
    }
}
