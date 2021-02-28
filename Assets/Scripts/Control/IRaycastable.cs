using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public interface IRaycastable
    {
        CursorType GetCursorType();
        bool HandleRaycast(PlayerController callingController, PlayerInputType inputType, PlayerInputType matchType);

        // Extended in IRaycastableExtension
        bool CheckDistanceTemplate();
    }
}
