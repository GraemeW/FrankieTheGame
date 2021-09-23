using Frankie.Core;
using Frankie.Speech;
using UnityEngine;

namespace Frankie.Control
{
    public class CheckWithMessage : Check
    {
        // Tunables
        [SerializeField] string checkMessage = "";
        // Events
        public InteractionEvent postMessageCheckInteraction;

        public override bool HandleRaycast(PlayerStateHandler playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (string.IsNullOrEmpty(checkMessage)) { return false; }

            if (!this.CheckDistance(gameObject, transform.position, playerController,
                overrideDefaultInteractionDistance, interactionDistance))
            {
                return false;
            }

            if (inputType == matchType)
            {
                playerStateHandler.EnterDialogue(checkMessage);
                SetupPostCheckActions(playerStateHandler);

                if (checkInteraction != null)
                {
                    checkInteraction.Invoke(playerStateHandler);
                }
            }
            return true;
        }

        private void SetupPostCheckActions(PlayerStateHandler playerStateHandler)
        {
            DialogueController dialogueController = playerStateHandler.GetCurrentDialogueController();
            if (dialogueController != null && postMessageCheckInteraction != null)
            {
                dialogueController.SetDestroyCallbackActions(postMessageCheckInteraction);
            }
        }
    }

}