using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Utils
{
    public interface IUIBoxCallbackReceiver
    {
        public void HandleDisableCallback(IUIBoxCallbackReceiver dialogueBox, Action action);
    }
}