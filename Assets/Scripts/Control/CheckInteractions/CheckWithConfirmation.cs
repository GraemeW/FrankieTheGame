using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public class CheckWithConfirmation : CheckWithMessage
    {
        // Tunables
        [SerializeField] string messageAccept = "OK!";
        [SerializeField] string messageReject = "Nah";
        [SerializeField][Tooltip("Optional action on reject")] InteractionEvent rejectInteraction = null;

        public override bool HandleRaycast(PlayerStateHandler playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (string.IsNullOrEmpty(checkMessage)) { return false; }
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                List<ChoiceActionPair> interactActions = new List<ChoiceActionPair>();
                interactActions.Add(new ChoiceActionPair(messageAccept, () => checkInteraction.Invoke(playerStateHandler)));
                interactActions.Add(new ChoiceActionPair(messageReject, () => rejectInteraction.Invoke(playerStateHandler)));

                playerStateHandler.EnterDialogue(checkMessage, interactActions);
            }
            return true;
        }


    }
}
