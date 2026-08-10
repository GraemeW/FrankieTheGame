using System.Collections.Generic;
using UnityEngine;
using LowDefMustard.Control;
using LowDefMustard.Utils;
using Frankie.Core;

namespace Frankie.Control
{
    public class CheckWithDynamicOptions : CheckBase
    {
        [SerializeField][Tooltip("Must implement ICheckDynamic")] private GameObject dynamicCheckObject;
        [SerializeField] private InteractionEvent checkInteraction;

        public override bool HandleRaycast(PlayerStateMachine playerStateMachine, PlayerController playerController, ControllerInputType inputType, ControllerInputType matchType)
        {
            if (dynamicCheckObject == null) { return false; }
            if (!dynamicCheckObject.TryGetComponent(out ICheckDynamic checkDynamic)) { return false; }

            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                List<ChoiceActionPair> interactActions = checkDynamic.GetChoiceActionPairs(playerStateMachine);
                if (interactActions == null) { return false; }
                if (interactActions.Count == 0) { return false; }

                checkInteraction?.Invoke(playerStateMachine);
                string message = checkDynamic.GetMessage();
                message ??= "";

                playerStateMachine.EnterDialogue(message, interactActions);
            }
            return true;
        }
    }
}
