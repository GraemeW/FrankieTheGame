using Frankie.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Frankie.Control
{
    [RequireComponent(typeof(Collider2D))]
    public class Check : MonoBehaviour, IRaycastable
    {
        // Tunables
        [SerializeField] protected bool overrideDefaultInteractionDistance = false;
        [SerializeField] protected float interactionDistance = 0.3f;

        // Events
        public InteractionEvent checkInteraction;

        // Data Structures
        [System.Serializable]
        public class InteractionEvent : UnityEvent<PlayerStateHandler>
        {
        }

        // Raycastable Interface Implementation
        public virtual CursorType GetCursorType()
        {
            return CursorType.Check;
        }

        public virtual bool HandleRaycast(PlayerStateHandler playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (!this.CheckDistance(gameObject, transform.position, playerController,
                overrideDefaultInteractionDistance, interactionDistance))
            {
                return false;
            }

            if (inputType == matchType)
            {
                if (checkInteraction != null)
                {
                    checkInteraction.Invoke(playerStateHandler);
                }
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