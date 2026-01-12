using UnityEngine;
using Frankie.Utils;

namespace Frankie.Control
{
    public interface IRaycastable
    {
        CursorType GetCursorType();
        bool HandleRaycast(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType);

        public static bool CheckDistance(GameObject gameObject, Vector2 position, PlayerController callingController, bool overrideDefaultInteractionDistance, float interactionDistance)
        {
            if (!overrideDefaultInteractionDistance) { interactionDistance = callingController.GetInteractionDistance(); }

            RaycastHit2D playerCastToObject = callingController.PlayerCastToObject(position);
            if (playerCastToObject.collider == null) { return false; }
            if (playerCastToObject.collider.transform.gameObject != gameObject) { return false; } // obstructed
            if (!SmartVector2.CheckDistance(callingController.GetInteractionPosition(), playerCastToObject.point, interactionDistance)) { return false; }
            
            return true;
        }
    }
}
