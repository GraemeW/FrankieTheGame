using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LowDefMustard.Control
{
    public abstract class BaseController : MonoBehaviour
    {
        // Tunables
        [SerializeField] private float listenerPollingInterval = 0.1f;
        
        // State
        protected float timeSinceLastPolled = 0f;
        protected bool destroyQueued = false;
        private readonly List<ActiveInputReceiver> activeInputReceivers = new();
        
        // Events
        private event Action<ControllerInputType> globalInput;
        
        #region UnityMethods
        protected virtual void LateUpdate()
        {
            if (ShouldDestroyForNoReceivers()) { PollForReceivers(Time.deltaTime); }
            if (destroyQueued) { Destroy(gameObject); }
        }
        #endregion
        
        #region StaticMethods
        // Note:  Internal for test visibility
        protected internal static bool ParseDirectionalInput(Vector2 directionalInput, ControllerInputType lastControllerInputType, out ControllerInputType newControllerInputType)
        {
            newControllerInputType = NavigationVectorToInputType(directionalInput);
            return newControllerInputType != lastControllerInputType;
        }
        
        private static ControllerInputType NavigationVectorToInputType(Vector2 navigationVector)
        {
            float verticalMagnitude = Vector2.Dot(navigationVector, Vector2.up);
            float horizontalMagnitude = Vector2.Dot(navigationVector, Vector2.right);
            float vectorSelect = Mathf.Abs(verticalMagnitude) - Mathf.Abs(horizontalMagnitude);

            return vectorSelect switch
            {
                > 0 => verticalMagnitude > 0 ? ControllerInputType.NavigateUp : ControllerInputType.NavigateDown,
                < 0 => horizontalMagnitude > 0 ?  ControllerInputType.NavigateRight : ControllerInputType.NavigateLeft,
                _ => ControllerInputType.DefaultNone
            };
        }

        public static bool TryInputTypeToNavigationVector(ControllerInputType controllerInputType, out Vector2 navigationVector)
        {
            switch (controllerInputType)
            {
                case ControllerInputType.NavigateUp: navigationVector = Vector2.up; return true;
                case ControllerInputType.NavigateDown: navigationVector = Vector2.down; return true;
                case ControllerInputType.NavigateLeft: navigationVector = Vector2.left; return true;
                case ControllerInputType.NavigateRight: navigationVector = Vector2.right; return true;
            }
            navigationVector = Vector2.zero; 
            return false;
        }
        #endregion
        
        #region PublicMethods
        public void AddInputReceiver(IInputReceiver inputReceiver, Action disableCallbacks)
        {
            if (inputReceiver == null) { return; }
            HandleInputReceiverAdded(inputReceiver, disableCallbacks);
        }
        #endregion
        
        #region GlobalInput
        protected virtual bool HasAlternateReceiversActive() => false;
        protected bool HasGlobalInput() => globalInput != null;
        private void SubscribeToGlobalInput(bool enable, IInputReceiver inputReceiver)
        {
            globalInput -= inputReceiver.GetInputHandler();
            if (enable) { globalInput += inputReceiver.GetInputHandler(); }
        }
        protected void TriggerGlobalInput(ControllerInputType controllerInputType)
        {
            if (globalInput == null) { return; }
            timeSinceLastPolled = 0f;
            globalInput.Invoke(controllerInputType);
        }
        #endregion
        
        #region ReceiverHandling
        protected abstract void OnNoReceiversIdentified();
        protected virtual bool ShouldDestroyForNoReceivers() => false;
        private void OnNoActiveReceivers()
        {
            if (destroyQueued) { return; }
            if (!HasAlternateReceiversActive())
            {
                // Reset state for any stashed && disabled receivers
                activeInputReceivers.Clear(); 
            }
            if (ShouldDestroyForNoReceivers()) { destroyQueued = true; }
        }

        private bool TryGetActiveInputReceiver(IInputReceiver inputReceiver, out ActiveInputReceiver activeInputReceiver)
        {
            activeInputReceiver = null;
            if (inputReceiver == null) { return false; }
            activeInputReceiver = activeInputReceivers.FirstOrDefault(activeInputReceiver => activeInputReceiver.inputReceiver == inputReceiver);
            return activeInputReceiver != null;
        }

        private bool TryGetLastActiveInputReceiver(out ActiveInputReceiver lastActiveInputReceiver)
        {
            while (true)
            {
                lastActiveInputReceiver = activeInputReceivers.LastOrDefault(receiver => receiver.isGameObjectEnabled);
                if (lastActiveInputReceiver is not { inputReceiver: null }) { return lastActiveInputReceiver != null; }
                activeInputReceivers.Remove(lastActiveInputReceiver);
            }
        }

        private void HandleInputReceiverAdded(IInputReceiver inputReceiver, Action disableCallbacks)
        {
            if (!inputReceiver.TrySetController(this)) { return; }
            SubscribeToGlobalInput(true, inputReceiver);
            inputReceiver.SubscribeToReceiverUpdates(true, OnReceiverModified);
            
            if (TryGetLastActiveInputReceiver(out ActiveInputReceiver lastActiveInputReceiver))
            {
                lastActiveInputReceiver.EnableInput(false);
            }
            
            var newActiveInputReceiver = new ActiveInputReceiver(inputReceiver, disableCallbacks); 
            activeInputReceivers.Add(newActiveInputReceiver);
            newActiveInputReceiver.EnableInput(true);
        }
        
        private void OnReceiverModified(ReceiverModifiedType receiverModifiedType, ReceiverModifiedData receiverModifiedData)
        {
            if (receiverModifiedData?.inputReceiver == null) { return; }

            switch (receiverModifiedType)
            {
                case ReceiverModifiedType.ClearDisableCallbacks:
                    HandleReceiverCallbacksDisabled(receiverModifiedData.inputReceiver);
                    break;
                case ReceiverModifiedType.ClientEnable:
                    HandleReceiverEnable(receiverModifiedData.inputReceiver);
                    break;
                case ReceiverModifiedType.ClientDisable:
                    HandleReceiverDisable(receiverModifiedData.inputReceiver);
                    break;
                case ReceiverModifiedType.ClientExit:
                    HandleReceiverDestroyed(receiverModifiedData.inputReceiver);
                    break;
            }
        }

        private void HandleReceiverCallbacksDisabled(IInputReceiver inputReceiver)
        {
            if (!TryGetActiveInputReceiver(inputReceiver, out ActiveInputReceiver activeInputReceiver)) { return; }
            activeInputReceiver.disableCallbacks = null;
        }

        private void HandleReceiverEnable(IInputReceiver inputReceiver)
        {
            if (!TryGetActiveInputReceiver(inputReceiver, out ActiveInputReceiver activeInputReceiver)) { return; }
            
            if (TryGetLastActiveInputReceiver(out ActiveInputReceiver lastActiveInputReceiver)) { lastActiveInputReceiver.EnableInput(false);}
            
            activeInputReceiver.isGameObjectEnabled = true;
            SubscribeToGlobalInput(true, activeInputReceiver.inputReceiver);
            activeInputReceiver.EnableInput(true);
        }
        
        private void HandleReceiverDisable(IInputReceiver inputReceiver)
        {
            SubscribeToGlobalInput(false, inputReceiver);
            if (!TryGetActiveInputReceiver(inputReceiver, out ActiveInputReceiver activeInputReceiver)) { return; }
            
            activeInputReceiver.disableCallbacks?.Invoke();
            activeInputReceiver.isGameObjectEnabled = false;
            
            if (TryGetLastActiveInputReceiver(out ActiveInputReceiver lastActiveInputReceiver)) { lastActiveInputReceiver.EnableInput(true); }
        }
        
        private void HandleReceiverDestroyed(IInputReceiver inputReceiver)
        {
            if (!TryGetActiveInputReceiver(inputReceiver, out ActiveInputReceiver activeInputReceiver)) { return; }
            activeInputReceiver.inputReceiver.SubscribeToReceiverUpdates(false, OnReceiverModified);
            
            // Early Exit:  No point updating, destruction imminent
            if (destroyQueued) { return; }
            
            activeInputReceivers.Remove(activeInputReceiver);
            if (!TryGetLastActiveInputReceiver(out ActiveInputReceiver _)) { OnNoActiveReceivers(); }
        }
        #endregion
        
        #region Singleton
        protected bool VerifyUnique()
        {
            // Include in Awake to ensure Singleton
            Type derivedType = GetType();
            var playerControllers = FindObjectsByType(derivedType);
            if (playerControllers.Length <= 1) { return true; }
            
            Destroy(gameObject);
            return false;
        }
        #endregion
        
        #region PrivateMethods
        // Note:  Internal for test visibility
        internal void PollForReceivers(float deltaTime)
        {
            timeSinceLastPolled += deltaTime;
            if (timeSinceLastPolled < listenerPollingInterval) { return; }
            timeSinceLastPolled = 0f;
            
            if (TryGetLastActiveInputReceiver(out ActiveInputReceiver _)) { return; }
            if (HasAlternateReceiversActive()) { return; }

            OnNoReceiversIdentified();
            
            Debug.LogWarning($"Identified rogue controller with no active receivers ({gameObject.name}), queuing for destroy.");
            destroyQueued = true;
        }
        #endregion
    }
}
