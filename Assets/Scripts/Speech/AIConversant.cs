using Frankie.Control;
using Frankie.Core;
using Frankie.Stats;
using System;
using UnityEngine;

namespace Frankie.Speech
{
    public class AIConversant : Check
    {
        // Tunables
        [SerializeField] Dialogue dialogue = null;

        private void Awake()
        {
            CheckForNPCControllerInParent();
        }

        private void CheckForNPCControllerInParent()
        {
            NPCStateHandler npcController = GetComponentInParent<NPCStateHandler>();
            if (npcController == null) { throw new ArgumentException("Parameter cannot be null", nameof(npcController)); }
        }

        private void Start()
        {
            dialogue?.OverrideSpeakerNames(null);
        }

        public Dialogue GetDialogue()
        {
            return dialogue;
        }

        public override bool HandleRaycast(PlayerStateHandler playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (dialogue == null) { return false; }

            if (!this.CheckDistance(gameObject, transform.position, playerController,
                overrideDefaultInteractionDistance, interactionDistance))
            {
                return false;
            }

            if (inputType == matchType)
            {
                checkInteraction?.Invoke(playerStateHandler);
                playerStateHandler.EnterDialogue(this, dialogue);
            }
            return true;
        }

        public override CursorType GetCursorType()
        {
            return CursorType.Talk;
        }
    }
}