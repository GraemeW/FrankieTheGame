using System;

namespace Frankie.Utils.UI
{
    public interface IUIBoxCallbackReceiver
    {
        public void HandleDisableCallback(IUIBoxCallbackReceiver dialogueBox, Action action);
    }
}