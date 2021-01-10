using Frankie.Control;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneHandler : MonoBehaviour, IRaycastable
{
    [SerializeField] bool overrideDefaultInteractionDistance = false;
    [SerializeField] float interactionDistance = 0.3f;

    public CursorType GetCursorType()
    {
        return CursorType.Zone;
    }

    public bool HandleRaycast(PlayerController callingController, string interactButtonOne = "Fire1", string interactButtonTwo = "Fire2")
    {
        if (!overrideDefaultInteractionDistance) { interactionDistance = callingController.GetInteractionDistance(); }

        RaycastHit2D playerCastToObject = callingController.PlayerCastToObject(transform.position);
        if (!playerCastToObject) { return false; }
        if (playerCastToObject.transform.gameObject != gameObject) { return false; } // obstructed
        if (Vector2.Distance(callingController.transform.position, playerCastToObject.point) > interactionDistance) { return false; }

        return true;
    }
}
