using Frankie.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    [CreateAssetMenu(fileName = "New Party Option Check Configuration", menuName = "CheckConfigurations/PartyOptions")]
    public class PartyOptionCheckConfiguration : CheckConfiguration
    {
        [SerializeField] string messagePartyOptions = "What do you want to do?";
        [SerializeField] bool toggleLeaderAdjust = true;
        [SerializeField] string optionLeaderAdjust = "Change party leader";
        [SerializeField] CheckConfiguration partyLeaderConfiguration = null;
        [SerializeField] bool toggleAddToParty = true;
        [SerializeField] string optionAddToParty = "Add to party";
        [SerializeField] CheckConfiguration addToPartyConfiguration = null;
        [SerializeField] bool toggleRemoveFromParty = true;
        [SerializeField] string optionRemoveFromParty = "Remove from party";
        [SerializeField] CheckConfiguration removeFromPartyConfiguration = null;

        public override List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateHandler, CheckWithConfiguration callingCheck)
        {
            List<ChoiceActionPair> interactActions = new List<ChoiceActionPair>();
            if (toggleLeaderAdjust)
            {
                AddDialogueSpawnOptionForConfiguration(ref interactActions, playerStateHandler, callingCheck, optionLeaderAdjust, partyLeaderConfiguration);
            }

            if (toggleAddToParty)
            {
                AddDialogueSpawnOptionForConfiguration(ref interactActions, playerStateHandler, callingCheck, optionAddToParty, addToPartyConfiguration);
            }

            if (toggleRemoveFromParty)
            {
                AddDialogueSpawnOptionForConfiguration(ref interactActions, playerStateHandler, callingCheck, optionRemoveFromParty, removeFromPartyConfiguration);
            }
            return interactActions;
        }

        public override string GetMessage()
        {
            return messagePartyOptions;
        }
    }
}