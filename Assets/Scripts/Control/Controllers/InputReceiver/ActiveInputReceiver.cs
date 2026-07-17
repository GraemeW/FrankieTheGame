using System;

namespace Frankie.Control
{
    public class ActiveInputReceiver
    {
        public bool isGameObjectEnabled;
        public readonly IInputReceiver inputReceiver;
        public readonly Action<ControllerInputType> inputHandler;
        public Action disableCallbacks;

        public ActiveInputReceiver(IInputReceiver inputReceiver, Action<ControllerInputType> inputHandler, Action disableCallbacks)
        {
            isGameObjectEnabled = inputReceiver.gameObject.activeSelf;
            this.inputReceiver = inputReceiver;
            this.inputHandler = inputHandler;
            this.disableCallbacks = disableCallbacks ?? (() => { });
        }
        
        public void EnableInput(bool active)
        {
            if (inputReceiver == null) { return; }
            inputReceiver.SetActiveInput(active);
        }
    }
}
