using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils;

namespace Frankie.Control
{
    [RequireComponent(typeof(Collider2D))]
    public class Check : CheckBase, IRaycastable
    {
        // Events
        [Header("Base Check Behaviour")]
        [SerializeField] CheckType checkType = CheckType.Simple;
        [SerializeField] protected InteractionEvent checkInteraction = null;
        [Header("Message Behaviour")]
        [SerializeField][Tooltip("Otherwise, checks at end of interaction")] bool checkAtStartOfInteraction = false;
        [SerializeField][Tooltip("Use {0} for party leader")] protected string checkMessage = "{0} has checked this object";
        [SerializeField] string defaultPartyLeaderName = "Frankie";
        [Header("Choice Behaviour")]
        [SerializeField] string messageAccept = "OK!";
        [SerializeField] string messageReject = "Nah";
        [SerializeField][Tooltip("Optional action on reject choice")] InteractionEvent rejectInteraction = null;

        #region Interfaces
        public override bool HandleRaycast(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            return checkType switch
            {
                CheckType.Simple => SimpleCheck(playerStateHandler, playerController, inputType, matchType),
                CheckType.Message => MessageCheck(playerStateHandler, playerController, inputType, matchType),
                CheckType.ChoiceConfirmation => ChoiceConfirmationCheck(playerStateHandler, playerController, inputType, matchType),
                _ => SimpleCheck(playerStateHandler, playerController, inputType, matchType),
            };
        }
        #endregion

        #region SpecificImplementation
        private bool SimpleCheck(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                checkInteraction?.Invoke(playerStateHandler);
            }
            return true;
        }

        private bool MessageCheck(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (string.IsNullOrEmpty(checkMessage)) { return false; }
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                string partyLeaderName = playerStateHandler.GetParty().GetPartyLeaderName();
                if (string.IsNullOrWhiteSpace(partyLeaderName)) { partyLeaderName = defaultPartyLeaderName; }

                playerStateHandler.EnterDialogue(string.Format(checkMessage, partyLeaderName));
                if (checkAtStartOfInteraction)
                {
                    checkInteraction?.Invoke(playerStateHandler);
                }
                else
                {
                    playerStateHandler.SetPostDialogueCallbackActions(checkInteraction);
                }
            }
            return true;
        }

        private bool ChoiceConfirmationCheck(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (string.IsNullOrEmpty(checkMessage)) { return false; }
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                List<ChoiceActionPair> interactActions = new List<ChoiceActionPair>();
                interactActions.Add(new ChoiceActionPair(messageAccept, () => checkInteraction.Invoke(playerStateHandler)));
                interactActions.Add(new ChoiceActionPair(messageReject, () => rejectInteraction.Invoke(playerStateHandler)));

                string partyLeaderName = playerStateHandler.GetParty().GetPartyLeaderName();
                playerStateHandler.EnterDialogue(string.Format(checkMessage, partyLeaderName), interactActions);
            }
            return true;
        }
        #endregion
    }
}