using Frankie.Core;
using Frankie.Speech;
using UnityEngine;

namespace Frankie.Control
{
    public class CheckWithMessage : Check
    {
        // Tunables
        [SerializeField] protected string checkMessage = "";
        [SerializeField] [Tooltip("Otherwise, checks at end of interaction")] bool checkAtStartOfInteraction = false;

        public override bool HandleRaycast(PlayerStateHandler playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (string.IsNullOrEmpty(checkMessage)) { return false; }
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                playerStateHandler.EnterDialogue(checkMessage);
                if (checkAtStartOfInteraction)
                {
                    if (checkInteraction != null)
                    {
                        checkInteraction.Invoke(playerStateHandler);
                    }
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