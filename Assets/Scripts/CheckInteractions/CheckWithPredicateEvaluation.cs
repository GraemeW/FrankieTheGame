using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEditor;
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
        [Header("TODO: Remove these after copying")]
        [SerializeField][Tooltip("Use {0} for party leader")] private string messageForConditionMet = "{0} checked the object.";
        [SerializeField][Tooltip("Use {0} for party leader")] private string messageForConditionFailed = "{0} failed to check the object.";
        
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

                playerStateHandler.EnterDialogue(string.Format(messageForConditionMet, partyLeaderName));
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

                playerStateHandler.EnterDialogue(string.Format(messageForConditionFailed, partyLeaderName));
                playerStateHandler.SetPostDialogueCallbackActions(checkInteractionConditionFailed);
            }
            else
            {
                checkInteractionConditionMet?.Invoke(playerStateHandler);
            }
        }
        #endregion
        
        // TODO:  Remove
        public void TempCreateCheckEntries()
        {
            string keyStem;
            string key;
            TableEntryReference tableEntryReference;
            
            keyStem = nameof(localizedMessageForConditionMet).Replace("localized", "");
            key = LocalizationTool.GenerateKindaUniqueKey(GetType(), gameObject, keyStem);
            tableEntryReference = key;
            if (localizedMessageForConditionMet == null || LocalizationTool.GetEnglishEntry(localizationTableType, localizedMessageForConditionMet.TableEntryReference) != messageForConditionMet)
            {
                LocalizationTool.AddUpdateEnglishEntry(localizationTableType, tableEntryReference, messageForConditionMet);
                LocalizationTool.SafelyUpdateReference(localizationTableType, localizedMessageForConditionMet, key);
            }
            
            keyStem = nameof(localizedMessageForConditionFailed).Replace("localized", "");
            key = LocalizationTool.GenerateKindaUniqueKey(GetType(), gameObject, keyStem);
            tableEntryReference = key;
            if (localizedMessageForConditionFailed == null || LocalizationTool.GetEnglishEntry(localizationTableType, localizedMessageForConditionFailed.TableEntryReference) != messageForConditionFailed)
            {
                LocalizationTool.AddUpdateEnglishEntry(localizationTableType, tableEntryReference, messageForConditionFailed);
                LocalizationTool.SafelyUpdateReference(localizationTableType, localizedMessageForConditionFailed, key);
            }
            
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }
    }
}
