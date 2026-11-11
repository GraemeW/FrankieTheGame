using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Control;
using LowDefMustard.GameStateModifiers;
using LowDefMustard.Localization;
using LowDefMustard.Utils;
using Frankie.Core;
using Frankie.Utils.Localization;

namespace Frankie.Control
{
    [ExecuteInEditMode]
    public class CheckWithPredicateEvaluation : CheckBase, IGameStateModifierHandler
    {
        [SerializeField] private Condition condition;
        [SerializeField] private bool useMessageOnConditionMet = false;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageForConditionMet;
        [SerializeField] private protected InteractionEvent checkInteractionConditionMet;
        [SerializeField] private bool useMessageOnConditionFailed = false;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageForConditionFailed;
        [SerializeField] private protected InteractionEvent checkInteractionConditionFailed;

        // Editor Field References
        internal const string conditionField = nameof(condition);

        // GameState Modifier Properties
        [SerializeField][HideInInspector] private string backingHandlerGUID;
        public string handlerGUID { get => backingHandlerGUID; set => backingHandlerGUID = value; }
        [SerializeField][HideInInspector] private int backingListHashCheck;
        public int modifierListHashCheck { get => backingListHashCheck; set => backingListHashCheck = value; }
        [SerializeField][HideInInspector] private bool backingHasGameStateModifiers;
        public bool hasGameStateModifiers { get => backingHasGameStateModifiers; set => backingHasGameStateModifiers = value; }
        [SerializeField][HideInInspector] private List<string> backingGameStateModifierGUIDs;
        public List<string> gameStateModifierGUIDs { get => backingGameStateModifierGUIDs; set => backingGameStateModifierGUIDs = value ?? new List<string>(); }

        #region UnityMethods
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

        #region Interfaces
        public override bool HandleRaycast(PlayerStateMachine playerStateMachine, PlayerController playerController, ControllerInputType inputType, ControllerInputType matchType)
        {
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                if (condition.Check(playerStateMachine.GetComponentsInChildren<IPredicateEvaluator>()))
                {
                    HandleConditionMet(playerStateMachine);
                }
                else
                {
                    HandleConditionFailed(playerStateMachine);
                }
            }
            return true;
        }
        
        public override List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedMessageForConditionMet.TableEntryReference,
                localizedMessageForConditionFailed.TableEntryReference
            };
        }
        
        public IList<GameStateModifier> GetGameStateModifiers() => IGameStateModifierPredicate.CollectGameStateModifiers(condition?.GetPredicates());
        #endregion

        #region PrivateMethods
        private void HandleConditionMet(PlayerStateMachine playerStateMachine)
        {
            if (useMessageOnConditionMet)
            {
                string partyLeaderName = playerStateMachine.GetParty().GetPartyLeaderName();
                if (string.IsNullOrWhiteSpace(partyLeaderName)) { partyLeaderName = defaultPartyLeaderName; }

                playerStateMachine.EnterDialogue(string.Format(localizedMessageForConditionMet.GetSafeLocalizedString(), partyLeaderName));
                playerStateMachine.SetPostDialogueCallbackActions(checkInteractionConditionMet);
            }
            else
            {
                checkInteractionConditionMet?.Invoke(playerStateMachine);
            }
        }
        private void HandleConditionFailed(PlayerStateMachine playerStateMachine)
        {
            if (useMessageOnConditionFailed)
            {
                string partyLeaderName = playerStateMachine.GetParty().GetPartyLeaderName();
                if (string.IsNullOrWhiteSpace(partyLeaderName)) { partyLeaderName = defaultPartyLeaderName; }

                playerStateMachine.EnterDialogue(string.Format(localizedMessageForConditionFailed.GetSafeLocalizedString(), partyLeaderName));
                playerStateMachine.SetPostDialogueCallbackActions(checkInteractionConditionFailed);
            }
            else
            {
                checkInteractionConditionFailed?.Invoke(playerStateMachine);
            }
        }
        #endregion
    }
}
