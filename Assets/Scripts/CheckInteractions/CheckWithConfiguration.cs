using System.Collections.Generic;
using UnityEngine;
using Frankie.Core;
using Frankie.Utils;

namespace Frankie.Control
{
    public class CheckWithConfiguration : CheckBase
    {
        [SerializeField] private CheckConfiguration checkConfiguration;

        public override bool HandleRaycast(PlayerStateMachine playerStateMachine, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                string message = checkConfiguration.GetMessage();
                message ??= "";
                List<ChoiceActionPair> interactActions = checkConfiguration.GetChoiceActionPairs(playerStateMachine, this);
                if (interactActions == null) { return false; }
                switch (interactActions.Count)
                {
                    case 0:
                        return false;
                    case 1:
                        interactActions[0].action?.Invoke();
                        break;
                    default:
                        playerStateMachine.EnterDialogue(message, interactActions);
                        break;
                }
            }
            return true;
        }
    }
}
