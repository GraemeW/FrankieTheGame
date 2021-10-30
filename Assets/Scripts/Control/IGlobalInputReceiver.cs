using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public interface IGlobalInputReceiver
    {
        bool HandleGlobalInput(PlayerInputType playerInputType);
    }
}
