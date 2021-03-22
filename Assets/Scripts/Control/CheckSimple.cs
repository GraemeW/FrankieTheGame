using Frankie.Core;
using UnityEngine;

namespace Frankie.Control
{
    public class CheckSimple : Check
    {
        [SerializeField] string checkMessage = "";

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
                playerStateHandler.OpenSimpleDialogue(checkMessage);
                if (checkInteraction != null)
                {
                    checkInteraction.Invoke(playerStateHandler);
                }
            }
            return true;
        }
    }

}