using Frankie.Combat;
using Frankie.Stats;
using Frankie.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    [CreateAssetMenu(fileName = "New Adjust Party Leader Check Configuration", menuName = "CheckConfigurations/AdjustLeader")]
    public class AdjustPartyLeaderCheckConfiguration : CheckConfiguration
    {
        [SerializeField] string messageAdjustLeader = "Who you want to take over?";

        public override List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateHandler playerStateHandler)
        {
            Party party = playerStateHandler.GetParty();
            List<ChoiceActionPair> interactActions = new List<ChoiceActionPair>();
            if (party.GetPartySize() == 1) { return interactActions; } // throw empty list to prevent option from triggering

            foreach (CombatParticipant character in party.GetParty())
            {
                interactActions.Add(new ChoiceActionPair(character.GetCombatName(),
                    () => party.SetPartyLeader(character)));
            }
            return interactActions;
        }

        public override string GetMessage()
        {
            return messageAdjustLeader;
        }
    }
}