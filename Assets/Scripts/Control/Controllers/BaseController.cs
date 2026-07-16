using System;
using UnityEngine;

namespace Frankie.Control
{
    public abstract class BaseController : MonoBehaviour
    {
        // Tunables
        [SerializeField] private float listenerPollingInterval = 0.1f;
        
        // State
        private bool globalInputActivated = false;
        protected float timeSinceLastPolled = 0f;
        protected bool destroyQueued = false;
        
        // Events
        private event Action<ControllerInputType> globalInput;
        
        #region UnityMethods
        protected virtual void LateUpdate()
        {
            PollForListeners(Time.deltaTime);
            if (destroyQueued) { Destroy(gameObject); }
        }
        #endregion
        
        #region StaticMethods
        protected static bool ParseDirectionalInput(Vector2 directionalInput, ControllerInputType lastControllerInputType, out ControllerInputType newControllerInputType)
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
        #endregion
        
        #region PublicMethods
        public void SubscribeToGlobalInput(bool enable, Action<ControllerInputType> action)
        {
            if (enable) { globalInputActivated = true; }
            
            globalInput -= action;
            if (enable) { globalInput += action; }
        }
        #endregion
        
        #region ProtectedMethods
        protected virtual bool HasListeners() => globalInput != null;
        protected virtual bool HasBeenActivated() => globalInputActivated;
        
        protected bool VerifyUnique()
        {
            // Include in Awake to ensure Singleton
            Type derivedType = GetType();
            var playerControllers = FindObjectsByType(derivedType);
            if (playerControllers.Length <= 1) { return true; }
            
            Destroy(gameObject);
            return false;
        }

        protected bool HasGlobalInput() => globalInput != null;
        protected void TriggerGlobalInput(ControllerInputType controllerInputType)
        {
            if (globalInput == null) { return; }
            timeSinceLastPolled = 0f;
            globalInput.Invoke(controllerInputType);
        }
        #endregion
        
        #region PrivateMethods
        private void PollForListeners(float deltaTime)
        {
            if (!HasBeenActivated()) { return; }
            
            timeSinceLastPolled += deltaTime;
            if (timeSinceLastPolled < listenerPollingInterval) { return; }
            timeSinceLastPolled = 0f;
            
            if (HasListeners()) { return; }
            
            Debug.LogWarning($"Identified rogue controller with no listeners ({gameObject.name}), queuing for destroy.");
            destroyQueued = true;
        }
        #endregion
    }
}
