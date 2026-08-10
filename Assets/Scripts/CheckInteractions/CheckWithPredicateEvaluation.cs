using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Control;
using LowDefMustard.Localization;
using LowDefMustard.Utils;
using Frankie.Core;

namespace Frankie.Control
{
    [ExecuteInEditMode]
    public class CheckWithPredicateEvaluation : CheckBase
    {
        [SerializeField] private Condition condition;
        [SerializeField] private bool useMessageOnConditionMet = false;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageForConditionMet;
        [SerializeField] private protected InteractionEvent checkInteractionConditionMet;
        [SerializeField] private bool useMessageOnConditionFailed = false;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageForConditionFailed;
        [SerializeField] private protected InteractionEvent checkInteractionConditionFailed;
        
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
                checkInteractionConditionMet?.Invoke(playerStateMachine);
            }
        }
        #endregion
    }
}
