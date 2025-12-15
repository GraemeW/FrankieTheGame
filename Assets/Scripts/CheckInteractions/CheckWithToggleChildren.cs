using System.Collections.Generic;
using UnityEngine;
using Frankie.Core;
using Frankie.Saving;
using Frankie.Stats;

namespace Frankie.Control
{
    public class CheckWithToggleChildren : CheckBase
    {
        // Tunables
        [Header("Hookups")]
        [SerializeField] private Transform parentTransformForToggling;
        [SerializeField][Tooltip("True for enable, false for disable")] private bool toggleToConditionMet = true;
        [SerializeField] private Condition condition;
        [Header("Messages")]
        [SerializeField][Tooltip("Use {0} for party leader")] private string messageOnToggle = "*CLICK* Oh, it looks like {0} got the door open";
        [SerializeField][Tooltip("Use {0} for party leader")] private string messageOnConditionNotMet = "Huh, it appears to be locked";
        [SerializeField] private string defaultPartyLeaderName = "Frankie";

        // Events
        [Header("Events")]
        [SerializeField] protected InteractionEvent checkInteraction;
        [SerializeField] protected InteractionEvent checkInteractionOnConditionNotMet;

        // State
        private bool childrenStateSetBySave = false;

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

        public override bool HandleRaycast(PlayerStateMachine playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                ToggleChildren(playerStateHandler);
            }
            return true;
        }

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

        public void BypassCheckCondition(PlayerStateMachine playerStateHandler) // Also called via Unity Events
        {
            foreach (Transform child in parentTransformForToggling)
            {
                child.gameObject.SetActive(toggleToConditionMet);
            }
            SetActiveCheck(false); // Disabling further interactions after toggling once -- also saved via CaptureState in parent class
            checkInteraction?.Invoke(playerStateHandler);
        }

        private bool CheckCondition(PlayerStateMachine playerStateHandler)
        {
            return condition != null && condition.Check(GetEvaluators(playerStateHandler));
        }

        private IEnumerable<IPredicateEvaluator> GetEvaluators(PlayerStateMachine playerStateHandler)
        {
            return playerStateHandler.GetComponentsInChildren<IPredicateEvaluator>();
        }

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
    }
}
