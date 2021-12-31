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
        [SerializeField] [Tooltip("Use {0} for party leader")] string messageOnToggle = "*CLICK* Oh, it looks like {0} got the door open";
        [SerializeField] [Tooltip("Use {0} for party leader")] string messageOnConditionNotMet = "Huh, it appears to be locked";
        [SerializeField] string defaultPartyLeaderName = "Frankie";
        [SerializeField] [Tooltip("True for enable, false for disable")] bool toggleToConditionMet = true;
        [SerializeField] Condition condition = null;

        // Events
        [SerializeField] protected InteractionEvent checkInteraction = null;
        [SerializeField] protected InteractionEvent checkInteractionOnConditionNotMet = null;

        public override bool HandleRaycast(PlayerStateHandler playerStateHandler, PlayerController playerController, PlayerInputType inputType, PlayerInputType matchType)
        {
            if (!IsInRange(playerController)) { return false; }

            if (inputType == matchType)
            {
                ToggleChildren(playerStateHandler);
            }
            return true;
        }

        private void ToggleChildren(PlayerStateHandler playerStateHandler)
        {
            if (transform.childCount == 0) { return; }

            string partyLeaderName = playerStateHandler.GetComponent<Party>()?.GetPartyLeaderName();
            partyLeaderName ??= defaultPartyLeaderName;

            if (CheckCondition(playerStateHandler))
            {
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(toggleToConditionMet);
                }
                SetActiveCheck(false);
                checkInteraction?.Invoke(playerStateHandler);
                playerStateHandler.EnterDialogue(string.Format(messageOnToggle, partyLeaderName));
            }
            else
            {
                checkInteractionOnConditionNotMet?.Invoke(playerStateHandler);
                playerStateHandler.EnterDialogue(string.Format(messageOnConditionNotMet, partyLeaderName));
            }
        }

        private bool CheckCondition(PlayerStateHandler playerStateHandler)
        {
            if (condition == null) { return false; }

            return condition.Check(GetEvaluators(playerStateHandler));
        }

        private IEnumerable<IPredicateEvaluator> GetEvaluators(PlayerStateHandler playerStateHandler)
        {
            return playerStateHandler.GetComponentsInChildren<IPredicateEvaluator>();
        }

        public override void RestoreState(SaveState state)
        {
            if (state == null) { return; }

            if (!(bool)state.GetState())
            {
                // Reset children, as condition was met on prior save
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(toggleToConditionMet);
                }
            }
            base.RestoreState(state);
        }
    }
}