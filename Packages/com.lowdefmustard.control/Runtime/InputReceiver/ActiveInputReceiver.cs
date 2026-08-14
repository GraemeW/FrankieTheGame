using System;

namespace LowDefMustard.Control
{
    public class ActiveInputReceiver
    {
        public bool isGameObjectEnabled;
        public readonly IInputReceiver inputReceiver;
        public Action disableCallbacks;

        public ActiveInputReceiver(IInputReceiver inputReceiver, Action disableCallbacks)
        {
            isGameObjectEnabled = inputReceiver.gameObject.activeSelf;
            this.inputReceiver = inputReceiver;
            this.disableCallbacks = disableCallbacks ?? (() => { });
        }
        
        public void EnableInput(bool active)
        {
            inputReceiver?.SetActiveInput(active);
        }
    }
}
