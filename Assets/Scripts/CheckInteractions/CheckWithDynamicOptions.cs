using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils;

namespace Frankie.Control
{
    public class CheckWithDynamicOptions : CheckBase
    {
        [SerializeField][Tooltip("Must implement ICheckDynamic")] GameObject dynamicCheckObject = null;
        [SerializeField] InteractionEvent checkInteraction = null;

        public override bool HandleRaycast(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (dynamicCheckObject == null) { return false; }
            if (!dynamicCheckObject.TryGetComponent(out ICheckDynamic checkDynamic)) { return false; }

            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                List<ChoiceActionPair> interactActions = checkDynamic.GetChoiceActionPairs(playerStateHandler);
                if (interactActions == null) { return false; }
                if (interactActions.Count == 0) { return false; }

                checkInteraction?.Invoke(playerStateHandler);
                string message = checkDynamic.GetMessage();
                message ??= "";

                playerStateHandler.EnterDialogue(message, interactActions);
            }
            return true;
        }
    }
}
