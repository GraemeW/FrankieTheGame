using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public interface IGlobalInput
    {
        void HandleGlobalInput(string interactButtonOne = "Fire1");
    }
}
