using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Speech.UI
{
    public interface IDialogueBoxCallbackReceiver
    {
        public void HandleDialogueCallback(DialogueBox dialogueBox, string callbackMessage);
    }
}