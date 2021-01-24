using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public interface IGlobalInput
    {
        void HandleInput(string interactButtonOne = "Fire1");
    }
}
