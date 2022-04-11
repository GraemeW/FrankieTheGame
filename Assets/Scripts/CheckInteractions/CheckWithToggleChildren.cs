using Frankie.Core;
using Frankie.Saving;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    public class CheckWithToggleChildren : CheckBase
    {
        // Tunables
        [Header("Hookups")]
        [SerializeField] Transform parentTransformForToggling = null;
        [SerializeField] [Tooltip("True for enable, false for disable")] bool toggleToConditionMet = true;
        [SerializeField] Condition condition = null;
        [Header("Messages")]
        [SerializeField] [Tooltip("Use {0} for party leader")] string messageOnToggle = "*CLICK* Oh, it looks like {0} got the door open";
        [SerializeField] [Tooltip("Use {0} for party leader")] string messageOnConditionNotMet = "Huh, it appears to be locked";
        [SerializeField] string defaultPartyLeaderName = "Frankie";

        // Events
        [Header("Events")]
        [SerializeField] protected InteractionEvent checkInteraction = null;
        [SerializeField] protected InteractionEvent checkInteractionOnConditionNotMet = null;

        // State
        bool childrenStateSetBySave = false;

        private void Start()
        {
            if (childrenStateSetBySave) { return; }
                // Ensure correct order of operations (insurance:  nominally save happens before since existing at end of Awake)

            if (parentTransformForToggling != null)
            {
                foreach (Transform child in parentTransformForToggling)
                {
                    child.gameObject.SetActive(!toggleToConditionMet);
                }
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

            string partyLeaderName = playerStateHandler.GetComponent<Party>()?.GetPartyLeaderName();
            partyLeaderName ??= defaultPartyLeaderName;

            if (parentTransformForToggling == null) { parentTransformForToggling = transform; }
            if (CheckCondition(playerStateHandler))
            {
                foreach (Transform child in parentTransformForToggling)
                {
                    child.gameObject.SetActive(toggleToConditionMet);
                }
                SetActiveCheck(false); // Disabling further interactions after toggling once -- also saved via CaptureState in parent class
                checkInteraction?.Invoke(playerStateHandler);
                playerStateHandler.EnterDialogue(string.Format(messageOnToggle, partyLeaderName));
            }
            else
            {
                checkInteractionOnConditionNotMet?.Invoke(playerStateHandler);
                playerStateHandler.EnterDialogue(string.Format(messageOnConditionNotMet, partyLeaderName));
            }
        }

        private bool CheckCondition(PlayerStateMachine playerStateHandler)
        {
            if (condition == null) { return false; }

            return condition.Check(GetEvaluators(playerStateHandler));
        }

        private IEnumerable<IPredicateEvaluator> GetEvaluators(PlayerStateMachine playerStateHandler)
        {
            return playerStateHandler.GetComponentsInChildren<IPredicateEvaluator>();
        }

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
    }
}