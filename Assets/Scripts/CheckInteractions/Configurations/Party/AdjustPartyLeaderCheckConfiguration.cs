using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Core;
using Frankie.Stats;
using Frankie.Utils;
using Frankie.Utils.Localization;

namespace Frankie.Control
{
    [CreateAssetMenu(fileName = "New Adjust Party Leader Check Configuration", menuName = "CheckConfigurations/Party/AdjustLeader", order = 5)]
    public class AdjustPartyLeaderCheckConfiguration : CheckConfiguration
    {
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageAdjustLeader;

        public override string GetMessage() => localizedMessageAdjustLeader.GetSafeLocalizedString();
        
        public override List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateMachine, CheckWithConfiguration callingCheck)
        {
            Party party = playerStateMachine.GetParty();
            var interactActions = new List<ChoiceActionPair>();
            if (party.GetPartySize() == 1) { return interactActions; } // throw empty list to prevent option from triggering

            interactActions.AddRange(party.GetMembers().Select(character => 
                new ChoiceActionPair(CharacterProperties.GetCharacterDisplayName(character), () => party.SetPartyLeader(character))));
            return interactActions;
        }
        
        public override List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedMessageAdjustLeader.TableEntryReference
            };
        }
    }
}
