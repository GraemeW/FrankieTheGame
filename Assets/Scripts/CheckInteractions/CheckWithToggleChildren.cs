using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Core;
using Frankie.Core.Predicates;
using Frankie.Saving;
using Frankie.Stats;
using Frankie.Utils.Localization;

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
        public override bool HandleRaycast(PlayerStateMachine playerStateMachine, PlayerController playerController, ControllerInputType inputType, ControllerInputType matchType)
        {
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                ToggleChildren(playerStateMachine);
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
        public void BypassCheckCondition(PlayerStateMachine playerStateMachine) // Called via Unity Events
        {
            BypassCheckConditionWithNoInteractionEvents();
            checkInteraction?.Invoke(playerStateMachine);
        }

        public void BypassCheckConditionWithNoInteractionEvents() // Called via Unity Events
        {
            foreach (Transform child in parentTransformForToggling)
            {
                child.gameObject.SetActive(toggleToConditionMet);
            }
            SetActiveCheck(false); // Disabling further interactions after toggling once -- also saved via CaptureState in parent class
        }
        #endregion
        
        #region PrivateMethods
        private bool CheckCondition(PlayerStateMachine playerStateMachine) => condition != null && condition.Check(GetEvaluators(playerStateMachine)); 
        private IEnumerable<IPredicateEvaluator> GetEvaluators(PlayerStateMachine playerStateMachine) => playerStateMachine.GetComponentsInChildren<IPredicateEvaluator>();
        
        private void ToggleChildren(PlayerStateMachine playerStateMachine)
        {
            if (transform.childCount == 0) { return; }
            
            if (parentTransformForToggling == null) { parentTransformForToggling = transform; }

            string partyLeaderName = playerStateMachine.GetComponent<Party>()?.GetPartyLeaderName();
            partyLeaderName ??= defaultPartyLeaderName;
            if (CheckCondition(playerStateMachine))
            {
                BypassCheckCondition(playerStateMachine);
                playerStateMachine.EnterDialogue(string.Format(localizedMessageOnToggle.GetSafeLocalizedString(), partyLeaderName));
            }
            else
            {
                checkInteractionOnConditionNotMet?.Invoke(playerStateMachine);
                playerStateMachine.EnterDialogue(string.Format(localizedMessageOnConditionNotMet.GetSafeLocalizedString(), partyLeaderName));
            }
        }
        #endregion
        
        #region SaveInterface
        public override void RestoreState(SaveState saveState)
        {
            if (saveState == null) { return; }

            if (saveState.TryGetState(out bool isActive) && !isActive)
            {
                // Reset children, as condition was met on prior save
                if (parentTransformForToggling == null) { parentTransformForToggling = transform; }
                foreach (Transform child in parentTransformForToggling)
                {
                    child.gameObject.SetActive(toggleToConditionMet);
                }
                childrenStateSetBySave = true;
            }
            base.RestoreState(saveState);
        }
        #endregion
    }
}
