using Frankie.Saving;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public abstract class CheckBase : MonoBehaviour, IRaycastable, ISaveable
    {
        // Tunables
        [SerializeField] protected bool overrideDefaultInteractionDistance = false;
        [SerializeField] protected float interactionDistance = 0.3f;

        // State
        bool activeCheck = true;

        // Static
        static string DEFAULT_LAYER_MASK = "Default";
        static string INACTIVE_LAYER_MASK = "Ignore Raycast";

        public virtual CursorType GetCursorType()
        {
            return CursorType.Check;
        }

        public abstract bool HandleRaycast(PlayerStateHandler playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType);

        protected bool IsInRange(PlayerController playerController)
        {
            if (!activeCheck) { return false; }

            if (!this.CheckDistance(gameObject, transform.position, playerController, overrideDefaultInteractionDistance, interactionDistance))
            {
                return false;
            }
            return true;
        }

        bool IRaycastable.CheckDistanceTemplate()
        {
            // Not evaluated -> IRaycastableExtension
            return false;
        }

        public void SetActiveCheck(bool enable)
        {
            activeCheck = enable;
            if (enable)
            {
                gameObject.layer = LayerMask.NameToLayer(DEFAULT_LAYER_MASK);
            }
            else
            {
                gameObject.layer = LayerMask.NameToLayer(INACTIVE_LAYER_MASK);
            }
        }

        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        public SaveState CaptureState()
        {
            SaveState saveState = new SaveState(GetLoadPriority(), activeCheck);
            return saveState;
        }

        public virtual void RestoreState(SaveState state)
        {
            SetActiveCheck((bool)state.GetState());
        }
    }
}