using System.Collections.Generic;
using Frankie.Utils;

namespace Frankie.Speech
{
    public class DialogueData
    {
        public DialogueDataType dialogueDataType { get; }
        public AIConversant aiConversant { get; }
        public Dialogue dialogue { get; }
        public string message { get; }
        public List<ChoiceActionPair> choiceActionPairs { get; }

        public DialogueData(AIConversant aiConversant, Dialogue dialogue)
        {
            dialogueDataType = DialogueDataType.StandardDialogue;
            this.aiConversant = aiConversant;
            this.dialogue = dialogue;
        }

        public DialogueData(string message)
        {
            dialogueDataType = DialogueDataType.SimpleText;
            this.message = message;
        }

        public DialogueData(string message, List<ChoiceActionPair> choiceActionPairs)
        {
            dialogueDataType = DialogueDataType.SimpleChoice;
            this.message = message;
            this.choiceActionPairs = choiceActionPairs;
        }
    }
}
