using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public interface IGlobalInput
    {
        bool HandleGlobalInput(string interactButtonOne = "Fire1");
        bool HandleGlobalInput(KeyCode interactKeyOne = KeyCode.E);
    }
}
