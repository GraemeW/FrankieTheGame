using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public interface IStandardPlayerInputCaller
    {
        public event Action<PlayerInputType> globalInput;

        // Extended in IStandardPlayerInputCallerExtension
        PlayerInputType NavigationVectorToInputTypeTemplate(Vector2 navigationVector);
    }
}
