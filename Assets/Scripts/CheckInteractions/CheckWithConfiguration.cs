using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils;

namespace Frankie.Control
{
    public class CheckWithConfiguration : CheckBase
    {
        [SerializeField] private CheckConfiguration checkConfiguration;

        public override bool HandleRaycast(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                string message = checkConfiguration.GetMessage();
                message ??= "";
                List<ChoiceActionPair> interactActions = checkConfiguration.GetChoiceActionPairs(playerStateHandler, this);
                if (interactActions == null) { return false; }
                switch (interactActions.Count)
                {
                    case 0:
                        return false;
                    case 1:
                        interactActions[0].action?.Invoke();
                        break;
                    default:
                        playerStateHandler.EnterDialogue(message, interactActions);
                        break;
                }
            }
            return true;
        }
    }
}
