using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Core;
using Frankie.Utils;

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
        public override bool HandleRaycast(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                if (condition.Check(playerStateHandler.GetComponentsInChildren<IPredicateEvaluator>()))
                {
                    HandleConditionMet(playerStateHandler);
                }
                else
                {
                    HandleConditionFailed(playerStateHandler);
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
        private void HandleConditionMet(PlayerStateMachine playerStateHandler)
        {
            if (useMessageOnConditionMet)
            {
                string partyLeaderName = playerStateHandler.GetParty().GetPartyLeaderName();
                if (string.IsNullOrWhiteSpace(partyLeaderName)) { partyLeaderName = defaultPartyLeaderName; }

                playerStateHandler.EnterDialogue(string.Format(localizedMessageForConditionMet.GetSafeLocalizedString(), partyLeaderName));
                playerStateHandler.SetPostDialogueCallbackActions(checkInteractionConditionMet);
            }
            else
            {
                checkInteractionConditionMet?.Invoke(playerStateHandler);
            }
        }
        private void HandleConditionFailed(PlayerStateMachine playerStateHandler)
        {
            if (useMessageOnConditionFailed)
            {
                string partyLeaderName = playerStateHandler.GetParty().GetPartyLeaderName();
                if (string.IsNullOrWhiteSpace(partyLeaderName)) { partyLeaderName = defaultPartyLeaderName; }

                playerStateHandler.EnterDialogue(string.Format(localizedMessageForConditionFailed.GetSafeLocalizedString(), partyLeaderName));
                playerStateHandler.SetPostDialogueCallbackActions(checkInteractionConditionFailed);
            }
            else
            {
                checkInteractionConditionMet?.Invoke(playerStateHandler);
            }
        }
        #endregion
    }
}
