using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public abstract class CheckBase : MonoBehaviour, IRaycastable
    {
        // Tunables
        [SerializeField] protected bool overrideDefaultInteractionDistance = false;
        [SerializeField] protected float interactionDistance = 0.3f;

        public virtual CursorType GetCursorType()
        {
            return CursorType.Check;
        }

        public abstract bool HandleRaycast(PlayerStateHandler playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType);

        protected bool IsInRange(PlayerController playerController)
        {
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
    }
}