using System.Collections;
using System.Collections.Generic;
using Frankie.Utils;

namespace Frankie.Speech
{
    public class DialogueData
    {
        public DialogueDataType dialogueDataType { get; } = default;
        public AIConversant aiConversant { get; } = null;
        public Dialogue dialogue { get; } = null;
        public string message { get; } = null;
        public List<ChoiceActionPair> choiceActionPairs { get; } = null;

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
