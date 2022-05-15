using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Core;

namespace Frankie.Control
{
    public class CheckWithPredicateEvaluation : CheckBase
    {
        [SerializeField] Condition condition = null;
        [SerializeField] string defaultPartyLeaderName = "Frankie";
        [SerializeField] bool useMessageOnConditionMet = false;
        [SerializeField][Tooltip("Use {0} for party leader")] string messageForConditionMet = "{0} checked the object.";
        [SerializeField] protected InteractionEvent checkInteractionConditionMet = null;
        [SerializeField] bool useMessageOnConditionFailed = false;
        [SerializeField][Tooltip("Use {0} for party leader")] string messageForConditionFailed = "{0} failed to check the object.";
        [SerializeField] protected InteractionEvent checkInteractionConditionFailed = null;

        public override bool HandleRaycast(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                if (condition.Check(playerStateHandler.GetComponentsInChildren<IPredicateEvaluator>()))
                {
                    HandleConditionMet(playerStateHandler);
                }
                else
                {
                    HandleConditionFailed(playerStateHandler);
                }
            }
            return true;
        }

        private void HandleConditionMet(PlayerStateMachine playerStateHandler)
        {
            if (useMessageOnConditionMet)
            {
                string partyLeaderName = playerStateHandler.GetParty().GetPartyLeaderName();
                if (string.IsNullOrWhiteSpace(partyLeaderName)) { partyLeaderName = defaultPartyLeaderName; }

                playerStateHandler.EnterDialogue(string.Format(messageForConditionMet, partyLeaderName));
                playerStateHandler.SetPostDialogueCallbackActions(checkInteractionConditionMet);
            }
            else
            {
                checkInteractionConditionMet?.Invoke(playerStateHandler);
            }
        }
        private void HandleConditionFailed(PlayerStateMachine playerStateHandler)
        {
            if (useMessageOnConditionFailed)
            {
                string partyLeaderName = playerStateHandler.GetParty().GetPartyLeaderName();
                if (string.IsNullOrWhiteSpace(partyLeaderName)) { partyLeaderName = defaultPartyLeaderName; }

                playerStateHandler.EnterDialogue(string.Format(messageForConditionFailed, partyLeaderName));
                playerStateHandler.SetPostDialogueCallbackActions(checkInteractionConditionFailed);
            }
            else
            {
                checkInteractionConditionMet?.Invoke(playerStateHandler);
            }
        }
    }
}