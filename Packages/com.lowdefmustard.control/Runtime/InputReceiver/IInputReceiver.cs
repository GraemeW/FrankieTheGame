using System;
using UnityEngine;

namespace LowDefMustard.Control
{
    public interface IInputReceiver
    {
        public GameObject gameObject { get; }
        public bool destroyQueued { get; set; }
        public Action<ControllerInputType> GetInputHandler();
        public void SubscribeToReceiverUpdates(bool enable, Action<ReceiverModifiedType, ReceiverModifiedData> action);
        public void SetActiveInput(bool active);
        public bool TrySetController(BaseController controller);
    }
}
