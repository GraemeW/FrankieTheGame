using Frankie.Combat;
using Frankie.Speech;
using Frankie.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public class CheckWithConfiguration : CheckBase
    {
        [SerializeField] CheckConfiguration checkConfiguration = null;

        public override bool HandleRaycast(PlayerStateHandler playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                string message = checkConfiguration.GetMessage();
                message ??= "";
                List<ChoiceActionPair> interactActions = checkConfiguration.GetChoiceActionPairs(playerStateHandler);
                if (interactActions == null) { return false; }
                if (interactActions.Count == 0) { return false; }

                if (interactActions.Count == 1)
                {
                    interactActions[0].action?.Invoke();
                }
                else
                {
                    playerStateHandler.EnterDialogue(message, interactActions);
                }
            }
            return true;
        }
    }
}
