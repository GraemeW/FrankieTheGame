using UnityEngine;
using Frankie.Saving;

namespace Frankie.Control
{
    public abstract class CheckBase : MonoBehaviour, IRaycastable, ISaveable
    {
        // Tunables
        [SerializeField] protected bool overrideDefaultInteractionDistance = false;
        [SerializeField] protected float interactionDistance = 0.3f;

        // State
        private bool activeCheck = true;

        // Static
        private const string _defaultLayerMask = "Default";
        private const string _inactiveLayerMask = "Ignore Raycast";

        protected bool IsInRange(PlayerController playerController)
        {
            if (!activeCheck) { return false; }
            return this.CheckDistance(gameObject, transform.position, playerController, overrideDefaultInteractionDistance, interactionDistance);
        }
        
        public void SetActiveCheck(bool enable) // Called via Unity Events
        {
            activeCheck = enable;
            gameObject.layer = LayerMask.NameToLayer(enable ? _defaultLayerMask : _inactiveLayerMask);
        }
        
        #region RaycastableInterface
        public virtual CursorType GetCursorType() => CursorType.Check;

        public abstract bool HandleRaycast(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType);

        // Not evaluated -> IRaycastableExtension
        bool IRaycastable.CheckDistanceTemplate() => false;
        #endregion

        #region SaveInterface
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;

        public SaveState CaptureState()
        {
            var saveState = new SaveState(GetLoadPriority(), activeCheck);
            return saveState;
        }

        public virtual void RestoreState(SaveState state)
        {
            SetActiveCheck((bool)state.GetState(typeof(bool)));
        }
        #endregion
    }
}
