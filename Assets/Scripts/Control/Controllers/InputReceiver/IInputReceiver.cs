using System;
using UnityEngine;

namespace Frankie.Control
{
    public interface IInputReceiver
    {
        public GameObject gameObject { get; }
        public void SetActiveInput(bool active);
        public bool TrySetController(BaseController controller, out Action<ControllerInputType> inputHandler);
        public void SubscribeToReceiverUpdates(bool enable, Action<ReceiverModifiedType, ReceiverModifiedData> action);
        bool HandleGlobalInput(ControllerInputType controllerInputType);
    }
}
