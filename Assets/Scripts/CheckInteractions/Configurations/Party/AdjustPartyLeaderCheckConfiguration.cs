using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Stats;
using Frankie.Utils;

namespace Frankie.Control
{
    [CreateAssetMenu(fileName = "New Adjust Party Leader Check Configuration", menuName = "CheckConfigurations/AdjustLeader")]
    public class AdjustPartyLeaderCheckConfiguration : CheckConfiguration
    {
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageAdjustLeader;

        public override string GetMessage() => localizedMessageAdjustLeader.GetSafeLocalizedString();
        
        public override List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateHandler, CheckWithConfiguration callingCheck)
        {
            Party party = playerStateHandler.GetParty();
            var interactActions = new List<ChoiceActionPair>();
            if (party.GetPartySize() == 1) { return interactActions; } // throw empty list to prevent option from triggering

            interactActions.AddRange(party.GetParty().Select(character => 
                new ChoiceActionPair(character.GetCharacterProperties().GetCharacterNamePretty(), () => party.SetPartyLeader(character))));
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
