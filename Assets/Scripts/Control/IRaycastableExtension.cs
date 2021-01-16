using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public static class IRaycastableExtension
    {
        public static bool CheckDistance(this IRaycastable raycastable, GameObject gameObject, Vector2 position, PlayerController callingController, bool overrideDefaultInteractionDistance, float interactionDistance)
        {
            if (!overrideDefaultInteractionDistance) { interactionDistance = callingController.GetInteractionDistance(); }

            RaycastHit2D playerCastToObject = callingController.PlayerCastToObject(position);
            if (playerCastToObject.collider == null) { return false; }
            if (playerCastToObject.collider.transform.gameObject != gameObject) { return false; } // obstructed
            if (Vector2.Distance(callingController.GetInteractionPosition(), playerCastToObject.point) > interactionDistance) { return false; }
            
            return true;
        }
    }
}
