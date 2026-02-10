using System;

namespace Frankie.Utils.UI
{
    public interface IUIBoxCallbackReceiver
    {
        public void HandleDisableCallback(IUIBoxCallbackReceiver callbackReceiver, Action action);
    }
}
