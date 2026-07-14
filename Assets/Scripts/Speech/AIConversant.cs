using UnityEngine;
using UnityEngine.Events;
using Frankie.Core;
using Frankie.Core.Predicates;
using Frankie.Control;
using Frankie.Saving;

namespace Frankie.Speech
{
    public class AIConversant : CheckBase, IPredicateEvaluator, ISaveable<int>
    {
        // Tunables
        [SerializeField] private Dialogue dialogue;
        [SerializeField] protected InteractionEvent checkInteraction;
        [SerializeField] private UnityEvent onExitDialogue;
        [SerializeField] private bool saveDialogueCount = false;

        // State
        private int dialogueCount = 0;
        
        // Cached References
        private PlayerStateMachine playerStateMachine;

        #region UnityMethods
        private void Start()
        {
            dialogue?.OverrideSpeakerNames(null);
        }
        #endregion

        #region PublicPrivateMethods
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
            if (playerStateType != PlayerStateType.InWorld || playerStateMachine == null) { return; }
            
            playerStateMachine.playerStateChanged -= HandlePlayerExitDialogue;
            onExitDialogue?.Invoke();
        }
        #endregion

        #region Interfaces
        // Check Interface
        public override bool HandleRaycast(PlayerStateMachine playerStateMachine, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (dialogue == null) { return false; }
            if (!IRaycastable.CheckDistance(gameObject, transform.position, playerController, overrideDefaultInteractionDistance, interactionDistance)) { return false; }

            if (inputType == matchType)
            {
                ForceInteractionEvent(playerStateMachine);
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
        
        // Save Interface (overrides CheckBase)
        public override SaveState CaptureState() => ManualGetStateFromData(dialogueCount);

        public override void RestoreState(SaveState saveState)
        {
            if (!saveDialogueCount) { return; }
            if (TryManualGetDataFromState(saveState, out int value)) { dialogueCount = value; }
        }

        public SaveState ManualGetStateFromData(int data)
        {
            if (!saveDialogueCount) { return null; }
            
            if (data < 0) { data = dialogueCount; }
            return new SaveState(GetLoadPriority(), data);
        }

        public bool TryManualGetDataFromState(SaveState saveState, out int value)
        {
            if (!saveDialogueCount) { value = 0; return false; }
            
            // Save Found Pass Back
            if (saveState != null && saveState.TryGetState(out value))
            {
                value = value >= 0 ? value : dialogueCount;
                return true;
            }
            
            // Default Pass Back
            value = dialogueCount;
            return saveDialogueCount;
        }
        #endregion
    }
}
