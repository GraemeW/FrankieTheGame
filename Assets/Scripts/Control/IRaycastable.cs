using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public interface IRaycastable
    {
        CursorType GetCursorType();
        bool HandleRaycast(PlayerController callingController, string interactButtonOne = "Fire1", string interactButtonTwo = "Fire2");
        bool HandleRaycast(PlayerController callingController, KeyCode interactKeyOne = KeyCode.E, KeyCode interactKeyTwo = KeyCode.Return);

        // Extended in IRaycastableExtension
        bool CheckDistanceTemplate();
    }
}
