using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Dialogue.UI
{
    public interface IDialogueBoxCallbackReceiver
    {
        public void HandleDialogueCallback(string callbackMessage);
    }
}