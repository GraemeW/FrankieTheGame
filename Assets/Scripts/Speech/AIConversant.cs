using Frankie.Control;
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
            NPCController npcController = GetComponentInParent<NPCController>();
            if (npcController == null) { throw new ArgumentException("Parameter cannot be null", nameof(npcController)); }
        }

        private void Start()
        {
            if (dialogue != null)
            {
                dialogue.OverrideSpeakerNames(null);
            }
        }

        public override bool HandleRaycast(PlayerController callingController, string interactButtonOne = "Fire1", string interactButtonTwo = "Fire2")
        {
            if (dialogue == null) { return false; }

            if (!this.CheckDistance(gameObject, transform.position, callingController,
                overrideDefaultInteractionDistance, interactionDistance))
            {
                return false;
            }

            if (Input.GetButtonDown(interactButtonOne))
            {
                if (checkInteraction != null)
                {
                    checkInteraction.Invoke(callingController);
                }
                callingController.EnterDialogue(this, dialogue);
            }
            return true;
        }

        public override bool HandleRaycast(PlayerController callingController, KeyCode interactKeyOne = KeyCode.E, KeyCode interactKeyTwo = KeyCode.Return)
        {
            if (dialogue == null) { return false; }

            if (!this.CheckDistance(gameObject, transform.position, callingController,
                overrideDefaultInteractionDistance, interactionDistance))
            {
                return false;
            }

            if (Input.GetKeyDown(interactKeyOne))
            {
                if (checkInteraction != null)
                {
                    checkInteraction.Invoke(callingController);
                }
                callingController.EnterDialogue(this, dialogue);
            }
            return true;
        }

        public override CursorType GetCursorType()
        {
            return CursorType.Talk;
        }
    }
}