using UnityEngine;
using Frankie.Control;
using Frankie.Core;

namespace Frankie.Speech
{
    public class AIConversant : CheckBase, IPredicateEvaluator
    {
        // Tunables
        [SerializeField] Dialogue dialogue = null;
        [SerializeField] protected InteractionEvent checkInteraction = null;

        // State
        int dialogueCount = 0;

        #region UnityMethods
        private void Start()
        {
            dialogue?.OverrideSpeakerNames(null);
        }
        #endregion

        #region PublicMethods
        public Dialogue GetDialogue() => dialogue;
        public int GetDialogueCount() => dialogueCount;
        public void ResetDialogueCount() => dialogueCount = 0;

        public void ForceInteractionEvent(PlayerStateMachine playerStateHandler) // Called via Unity Events
        {
            checkInteraction?.Invoke(playerStateHandler);
            dialogueCount++;
            playerStateHandler.EnterDialogue(this, dialogue);
        }
        #endregion

        #region Interfaces
        // Check Interface
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

        // Predicate Interface
        public bool? Evaluate(Predicate predicate)
        {
            PredicateAIConversant predicateAIConversant = predicate as PredicateAIConversant;
            return predicateAIConversant != null ? predicateAIConversant.Evaluate(this) : null;
        }
        #endregion
    }
}
