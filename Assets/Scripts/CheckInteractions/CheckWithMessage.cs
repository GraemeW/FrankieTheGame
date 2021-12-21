using Frankie.Core;
using Frankie.Speech;
using Frankie.Stats;
using Frankie.Combat;
using UnityEngine;

namespace Frankie.Control
{
    public class CheckWithMessage : Check
    {
        // Tunables
        [SerializeField] [Tooltip("Use {0} for party leader")] protected string checkMessage = "{0} has checked this object";
        [SerializeField] string defaultPartyLeaderName = "Frankie";
        [SerializeField] [Tooltip("Otherwise, checks at end of interaction")] bool checkAtStartOfInteraction = false;

        public override bool HandleRaycast(PlayerStateHandler playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (string.IsNullOrEmpty(checkMessage)) { return false; }
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                string partyLeaderName = playerStateHandler.GetParty().GetParty()[0].GetCombatName();
                if (string.IsNullOrWhiteSpace(partyLeaderName)) { partyLeaderName = defaultPartyLeaderName; }

                playerStateHandler.EnterDialogue(string.Format(checkMessage, partyLeaderName));
                if (checkAtStartOfInteraction)
                {
                    checkInteraction?.Invoke(playerStateHandler);
                }
                else
                {
                    SetupPostCheckActions(playerStateHandler);
                }
            }
            return true;
        }

        protected void SetupPostCheckActions(PlayerStateHandler playerStateHandler)
        {
            DialogueController dialogueController = playerStateHandler.GetCurrentDialogueController();
            if (dialogueController != null && checkInteraction != null)
            {
                dialogueController.SetDestroyCallbackActions(checkInteraction);
            }
        }
    }

}