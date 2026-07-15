using Frankie.Speech;

namespace Frankie.Core.PlayerStateMemory
{
    public class DialogueMemory
    {
        public DialogueData dialogueData;

        public bool InitiateDialogue(DialogueController dialogueController)
        {
            if (dialogueController == null || dialogueData == null) { return false; }
            switch (dialogueData.dialogueDataType)
            {
                case DialogueDataType.StandardDialogue:
                    dialogueController.InitiateConversation(dialogueData.aiConversant, dialogueData.dialogue);
                    break;
                case DialogueDataType.SimpleText:
                    dialogueController.InitiateSimpleMessage(dialogueData.message);
                    break;
                case DialogueDataType.SimpleChoice:
                    dialogueController.InitiateSimpleOption(dialogueData.message, dialogueData.choiceActionPairs);
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}
