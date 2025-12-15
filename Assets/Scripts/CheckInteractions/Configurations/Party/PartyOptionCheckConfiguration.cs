using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils;

namespace Frankie.Control
{
    [CreateAssetMenu(fileName = "New Party Option Check Configuration", menuName = "CheckConfigurations/PartyOptions")]
    public class PartyOptionCheckConfiguration : CheckConfiguration
    {
        [SerializeField] private string messagePartyOptions = "What do you want to do?";
        [SerializeField] private bool toggleLeaderAdjust = true;
        [SerializeField] private string optionLeaderAdjust = "Change party leader";
        [SerializeField] private CheckConfiguration partyLeaderConfiguration;
        [SerializeField] private bool toggleAddToParty = true;
        [SerializeField] private string optionAddToParty = "Add to party";
        [SerializeField] private CheckConfiguration addToPartyConfiguration;
        [SerializeField] private bool toggleRemoveFromParty = true;
        [SerializeField] private string optionRemoveFromParty = "Remove from party";
        [SerializeField] private CheckConfiguration removeFromPartyConfiguration;

        public override string GetMessage() => messagePartyOptions;
        
        public override List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateHandler, CheckWithConfiguration callingCheck)
        {
            var interactActions = new List<ChoiceActionPair>();
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
    }
}
