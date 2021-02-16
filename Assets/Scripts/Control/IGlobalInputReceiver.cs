using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public interface IGlobalInputReceiver
    {
        void HandleGlobalInput(PlayerInputType playerInputType);
    }
}
