using UnityEngine;
using Frankie.Core;
using Frankie.Utils;

namespace Frankie.Control
{
    [ExecuteInEditMode]
    public class CheckWithPredicateEvaluation : CheckBase, ILocalizable
    {
        [SerializeField] private Condition condition;
        [SerializeField] private string defaultPartyLeaderName = "Frankie";
        [SerializeField] private bool useMessageOnConditionMet = false;
        [SerializeField][Tooltip("Use {0} for party leader")] private string messageForConditionMet = "{0} checked the object.";
        [SerializeField] private protected InteractionEvent checkInteractionConditionMet;
        [SerializeField] private bool useMessageOnConditionFailed = false;
        [SerializeField][Tooltip("Use {0} for party leader")] private string messageForConditionFailed = "{0} failed to check the object.";
        [SerializeField] private protected InteractionEvent checkInteractionConditionFailed;

        public void HandleDeletion()
        {
            // TODO:  Implement
        }
        
        protected void OnDestroy()
        {
            ILocalizable.TriggerOnDestroy(this);
        }
        
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
