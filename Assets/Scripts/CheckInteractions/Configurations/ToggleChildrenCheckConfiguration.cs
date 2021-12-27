using Frankie.Core;
using Frankie.Stats;
using Frankie.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    [CreateAssetMenu(fileName = "New Toggle Check Configuration", menuName = "CheckConfigurations/ToggleChildObjects")]
    public class ToggleChildrenCheckConfiguration : CheckConfiguration
    {
        // Tunables
        [SerializeField] [Tooltip("Use {0} for party leader")] string messageOnToggle = "*CLICK* Oh, it looks like {0} got the door open";
        [SerializeField] [Tooltip("Use {0} for party leader")] string messageOnConditionNotMet = "Huh, it appears to be locked";
        [SerializeField] [Tooltip("True for enable, false for disable")] bool toggleToConditionMet = true;
        [SerializeField] Condition condition = null;

        // Static
        static string DEFAULT_OPTION_TEXT = "Toggle";
        static string DEFAULT_PARTY_LEADER_NAME = "Frankie";

        public override List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateHandler playerStateHandler, CheckWithConfiguration callingCheck)
        {
            List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
            ChoiceActionPair choiceActionPair = new ChoiceActionPair(DEFAULT_OPTION_TEXT, () => ToggleChildren(playerStateHandler, callingCheck));
                // Single check implementation, option text never called
            choiceActionPairs.Add(choiceActionPair);
            return choiceActionPairs;
        }

        public override string GetMessage()
        {
            // Single check implementation, message never called
            return DEFAULT_OPTION_TEXT;
        }

        private void ToggleChildren(PlayerStateHandler playerStateHandler, CheckWithConfiguration callingCheck)
        {
            if (callingCheck.transform.childCount == 0) { return; }

            string partyLeaderName = playerStateHandler.GetComponent<Party>()?.GetPartyLeaderName();
            partyLeaderName ??= DEFAULT_PARTY_LEADER_NAME;

            if (CheckCondition(playerStateHandler))
            {
                foreach(Transform child in callingCheck.transform)
                {
                    child.gameObject.SetActive(toggleToConditionMet);
                }
                playerStateHandler.EnterDialogue(string.Format(messageOnToggle, partyLeaderName));
                callingCheck.SetActiveCheck(false);
            }
            else
            {
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
    }
}