using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public interface IStandardPlayerInputCaller
    {
        public event Action<PlayerInputType> globalInput;
        void VerifyUnique(); // Define and call in awake, each controller should be a singleton

        // Extended in IStandardPlayerInputCallerExtension
        PlayerInputType NavigationVectorToInputTypeTemplate(Vector2 navigationVector);
    }
}
