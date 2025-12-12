using System.Collections.Generic;
using UnityEngine;
using Frankie.Stats;
using Frankie.Utils;

namespace Frankie.Control
{
    [CreateAssetMenu(fileName = "New Adjust Party Leader Check Configuration", menuName = "CheckConfigurations/AdjustLeader")]
    public class AdjustPartyLeaderCheckConfiguration : CheckConfiguration
    {
        [SerializeField] private string messageAdjustLeader = "Who you want to take over?";

        public override string GetMessage() => messageAdjustLeader;
        
        public override List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateHandler, CheckWithConfiguration callingCheck)
        {
            Party party = playerStateHandler.GetParty();
            var interactActions = new List<ChoiceActionPair>();
            if (party.GetPartySize() == 1) { return interactActions; } // throw empty list to prevent option from triggering

            foreach (BaseStats character in party.GetParty())
            {
                interactActions.Add(new ChoiceActionPair(character.GetCharacterProperties().GetCharacterNamePretty(),
                    () => party.SetPartyLeader(character)));
            }
            return interactActions;
        }
    }
}
