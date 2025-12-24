using UnityEngine;
using UnityEngine.Events;
using Frankie.Control;
using Frankie.Core;

namespace Frankie.Speech
{
    public class AIConversant : CheckBase, IPredicateEvaluator
    {
        // Tunables
        [SerializeField] private Dialogue dialogue;
        [SerializeField] protected InteractionEvent checkInteraction;
        [SerializeField] private UnityEvent onExitDialogue;

        // State
        private int dialogueCount = 0;
        
        // Cached References
        PlayerStateMachine playerStateMachine;

        #region UnityMethods
        private void Start()
        {
            dialogue?.OverrideSpeakerNames(null);
        }
        #endregion

        #region PublicPrivateMethods
        public Dialogue GetDialogue() => dialogue;
        public int GetDialogueCount() => dialogueCount;
        public void ResetDialogueCount() => dialogueCount = 0;

        public void ForceInteractionEvent(PlayerStateMachine setPlayerStateMachine) // Called via Unity Events
        {
            playerStateMachine = setPlayerStateMachine;
            
            checkInteraction?.Invoke(setPlayerStateMachine);
            dialogueCount++;
            playerStateMachine.EnterDialogue(this, dialogue);
            
            playerStateMachine.playerStateChanged += HandlePlayerExitDialogue;
        }

        private void HandlePlayerExitDialogue(PlayerStateType playerStateType, IPlayerStateContext playerStateContext)
        {
            if (playerStateType != PlayerStateType.inWorld || playerStateMachine == null) { return; }
            
            playerStateMachine.playerStateChanged -= HandlePlayerExitDialogue;
            onExitDialogue?.Invoke();
        }
        #endregion

        #region Interfaces
        // Check Interface
        public override bool HandleRaycast(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (dialogue == null) { return false; }
            if (!this.CheckDistance(gameObject, transform.position, playerController, overrideDefaultInteractionDistance, interactionDistance)) { return false; }

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
            var predicateAIConversant = predicate as PredicateAIConversant;
            return predicateAIConversant != null ? predicateAIConversant.Evaluate(this) : null;
        }
        #endregion
    }
}
