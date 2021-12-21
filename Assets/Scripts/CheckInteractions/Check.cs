using Frankie.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Frankie.Control
{
    [RequireComponent(typeof(Collider2D))]
    public class Check : CheckBase, IRaycastable
    {
        // Events
        [SerializeField] protected InteractionEvent checkInteraction = null;

        // Raycastable Interface Implementation

        public override bool HandleRaycast(PlayerStateHandler playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                checkInteraction?.Invoke(playerStateHandler);
            }
            return true;
        }
    }
}