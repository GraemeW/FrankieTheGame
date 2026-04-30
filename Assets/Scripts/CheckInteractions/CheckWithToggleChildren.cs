using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Core;
using Frankie.Saving;
using Frankie.Stats;
using Frankie.Utils;

namespace Frankie.Control
{
    [ExecuteInEditMode]
    public class CheckWithToggleChildren : CheckBase
    {
        // Tunables
        [Header("Hookups")]
        [SerializeField] private Transform parentTransformForToggling;
        [SerializeField][Tooltip("True for enable, false for disable")] private bool toggleToConditionMet = true;
        [SerializeField] private Condition condition;
        [Header("Messages")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageOnToggle;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageOnConditionNotMet;
        [Header("TODO: Remove these after copying")]
        [SerializeField][Tooltip("Use {0} for party leader")] private string messageOnToggle = "*CLICK* Oh, it looks like {0} got the door open";
        [SerializeField][Tooltip("Use {0} for party leader")] private string messageOnConditionNotMet = "Huh, it appears to be locked";

        // Events
        [Header("Events")]
        [SerializeField] protected InteractionEvent checkInteraction;
        [SerializeField] protected InteractionEvent checkInteractionOnConditionNotMet;

        // State
        private bool childrenStateSetBySave = false;

        #region UnityMethods
        private void Start()
        {
            if (childrenStateSetBySave) { return; }
            // Ensure correct order of operations (insurance:  nominally save happens before since existing at end of Awake)

            if (parentTransformForToggling == null) return;
            foreach (Transform child in parentTransformForToggling)
            {
                child.gameObject.SetActive(!toggleToConditionMet);
            }
        }
        #endregion

        #region OtherInterfaces
        public override bool HandleRaycast(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                ToggleChildren(playerStateHandler);
            }
            return true;
        }
        
        public override List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedMessageOnToggle.TableEntryReference,
                localizedMessageOnConditionNotMet.TableEntryReference
            };
        }
        #endregion
        
        #region PublicMethods
        public void BypassCheckCondition(PlayerStateMachine playerStateHandler) // Also called via Unity Events
        {
            BypassCheckConditionWithNoInteractionEvents();
            checkInteraction?.Invoke(playerStateHandler);
        }

        public void BypassCheckConditionWithNoInteractionEvents() // Also called via Unity Events
        {
            foreach (Transform child in parentTransformForToggling)
            {
                child.gameObject.SetActive(toggleToConditionMet);
            }
            SetActiveCheck(false); // Disabling further interactions after toggling once -- also saved via CaptureState in parent class
        }
        #endregion
        
        #region PrivateMethods
        private bool CheckCondition(PlayerStateMachine playerStateHandler) => condition != null && condition.Check(GetEvaluators(playerStateHandler)); 
        private IEnumerable<IPredicateEvaluator> GetEvaluators(PlayerStateMachine playerStateHandler) => playerStateHandler.GetComponentsInChildren<IPredicateEvaluator>();
        
        private void ToggleChildren(PlayerStateMachine playerStateHandler)
        {
            if (transform.childCount == 0) { return; }
            
            if (parentTransformForToggling == null) { parentTransformForToggling = transform; }

            string partyLeaderName = playerStateHandler.GetComponent<Party>()?.GetPartyLeaderName();
            partyLeaderName ??= defaultPartyLeaderName;
            if (CheckCondition(playerStateHandler))
            {
                BypassCheckCondition(playerStateHandler);
                playerStateHandler.EnterDialogue(string.Format(messageOnToggle, partyLeaderName));
            }
            else
            {
                checkInteractionOnConditionNotMet?.Invoke(playerStateHandler);
                playerStateHandler.EnterDialogue(string.Format(messageOnConditionNotMet, partyLeaderName));
            }
        }
        #endregion
        
        #region SaveInterface
        public override void RestoreState(SaveState state)
        {
            if (state == null) { return; }

            if (!(bool)state.GetState(typeof(bool)))
            {
                // Reset children, as condition was met on prior save
                if (parentTransformForToggling == null) { parentTransformForToggling = transform; }
                foreach (Transform child in parentTransformForToggling)
                {
                    child.gameObject.SetActive(toggleToConditionMet);
                }
                childrenStateSetBySave = true;
            }
            base.RestoreState(state);
        }
        #endregion


        public void TempCreateCheckEntries()
        {
            string keyStem;
            string key;
            TableEntryReference tableEntryReference;

            keyStem = nameof(localizedMessageOnToggle).Replace("localized", "");
            key = LocalizationTool.GenerateKindaUniqueKey(GetType(), gameObject, keyStem);
            tableEntryReference = key;
            if (localizedMessageOnToggle == null || LocalizationTool.GetEnglishEntry(localizationTableType, localizedMessageOnToggle.TableEntryReference) != messageOnToggle)
            {
                LocalizationTool.AddUpdateEnglishEntry(localizationTableType, tableEntryReference, messageOnToggle);
                LocalizationTool.SafelyUpdateReference(localizationTableType, localizedMessageOnToggle, key);
            }
            
            keyStem = nameof(localizedMessageOnConditionNotMet).Replace("localized", "");
            key = LocalizationTool.GenerateKindaUniqueKey(GetType(), gameObject, keyStem);
            tableEntryReference = key;
            if (localizedMessageOnConditionNotMet == null || LocalizationTool.GetEnglishEntry(localizationTableType, localizedMessageOnConditionNotMet.TableEntryReference) != messageOnConditionNotMet)
            {
                LocalizationTool.AddUpdateEnglishEntry(localizationTableType, tableEntryReference, messageOnConditionNotMet);
                LocalizationTool.SafelyUpdateReference(localizationTableType, localizedMessageOnConditionNotMet, key);
            }
            
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }
    }
}
