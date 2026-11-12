using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using LowDefMustard.Control;
using LowDefMustard.GameStateModifiers;
using LowDefMustard.Saving;
using LowDefMustard.Utils;
using Frankie.Core;
using Frankie.Control;

namespace Frankie.Speech
{
    [ExecuteInEditMode]
    public class AIConversant : CheckBase, IPredicateEvaluator, ISaveable<int>, IGameStateModifierHandler
    {
        // Note:  Internal methods/properties are for test visibility
        
        // Tunables
        [SerializeField] private Dialogue dialogue;
        [SerializeField] protected InteractionEvent checkInteraction;
        [SerializeField] private UnityEvent onExitDialogue;
        [SerializeField] private bool saveDialogueCount = false;

        // Editor Field References
        internal const string dialogueField = nameof(dialogue);

        // GameState Modifier Properties
        [SerializeField][HideInInspector] private string backingHandlerGUID;
        public string handlerGUID { get => backingHandlerGUID; set => backingHandlerGUID = value; }
        [SerializeField][HideInInspector] private int backingListHashCheck;
        public int modifierListHashCheck { get => backingListHashCheck; set => backingListHashCheck = value; }
        [SerializeField][HideInInspector] private bool backingHasGameStateModifiers;
        public bool hasGameStateModifiers { get => backingHasGameStateModifiers; set => backingHasGameStateModifiers = value; }
        [SerializeField][HideInInspector] private List<string> backingGameStateModifierGUIDs;
        public List<string> gameStateModifierGUIDs { get => backingGameStateModifierGUIDs; set => backingGameStateModifierGUIDs = value ?? new List<string>(); } 
        
        // State
        private int dialogueCount = 0;

        #region UnityMethods
        private void Start()
        {
            if (!Application.isPlaying) { return; } // [ExecuteInEditMode] -> don't leave runtime speaker overrides on the shared dialogue asset
            if (dialogue != null) { dialogue.OverrideSpeakerNames(null); }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            IGameStateModifierHandler.TriggerOnDestroy(this);
        }
        
        private void OnDrawGizmos()
        {
            IGameStateModifierHandler.TriggerOnGizmos(this);
        }
        #endregion

        #region PublicPrivateMethods
        public int GetDialogueCount() => dialogueCount;
        public void ResetDialogueCount() => dialogueCount = 0;

        public void ForceInteractionEvent(PlayerStateMachine playerStateMachine) // Called via Unity Events
        {
            checkInteraction?.Invoke(playerStateMachine);
            dialogueCount++;
            playerStateMachine.EnterDialogue(this, dialogue);
        }

        // Raised by the DialogueController that ran this conversant's dialogue, once that dialogue ends
        internal void NotifyDialogueExited() => onExitDialogue?.Invoke();
        
        internal UnityEvent GetOnExitDialogue() => onExitDialogue ??= new UnityEvent();
        #endregion

        #region Interfaces
        // Check Interface
        public override bool HandleRaycast(PlayerStateMachine passPlayerStateMachine, PlayerController playerController, ControllerInputType inputType, ControllerInputType matchType)
        {
            if (dialogue == null) { return false; }
            if (!IRaycastable.CheckDistance(gameObject, transform.position, playerController, overrideDefaultInteractionDistance, interactionDistance)) { return false; }

            if (inputType == matchType)
            {
                ForceInteractionEvent(passPlayerStateMachine);
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
        
        // GameStateModifierHandler Interface
        public IList<GameStateModifier> GetGameStateModifiers()
        {
            return IGameStateModifierPredicate.CollectGameStateModifiers(dialogue != null ? dialogue.GetAllPredicates() : null);
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
