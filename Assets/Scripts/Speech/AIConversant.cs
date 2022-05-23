using Frankie.Control;
using Frankie.Core;
using Frankie.Stats;
using System;
using UnityEngine;

namespace Frankie.Speech
{
    public class AIConversant : CheckBase
    {
        // Tunables
        [SerializeField] Dialogue dialogue = null;
        [SerializeField] protected InteractionEvent checkInteraction = null;

        private void Start()
        {
            dialogue?.OverrideSpeakerNames(null);
        }

        public Dialogue GetDialogue()
        {
            return dialogue;
        }

        public void ForceInteractionEvent(PlayerStateMachine playerStateHandler) // Called via Unity Events
        {
            checkInteraction?.Invoke(playerStateHandler);
            playerStateHandler.EnterDialogue(this, dialogue);
        }

        public override bool HandleRaycast(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (dialogue == null) { return false; }

            if (!this.CheckDistance(gameObject, transform.position, playerController,
                overrideDefaultInteractionDistance, interactionDistance))
            {
                return false;
            }

            if (inputType == matchType)
            {
                ForceInteractionEvent(playerStateHandler);
            }
            return true;
        }

        public override CursorType GetCursorType()
        {
            return CursorType.Talk;
        }
    }
}