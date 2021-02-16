using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public interface IStandardPlayerInputCaller
    {
        PlayerInputType GetPlayerInput();

        public event Action<PlayerInputType> globalInput;
    }
}
