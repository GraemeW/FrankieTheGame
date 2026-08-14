using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Core;
using Frankie.Stats;

namespace Frankie.Control
{
    [CreateAssetMenu(fileName = "New Add to Party Check Configuration", menuName = "CheckConfigurations/Party/AddToParty", order = 5)]
    public class AddPartyCharacterCheckConfiguration : CheckConfiguration
    {
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageAddToParty;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessagePartyFull;

        public override string GetMessage() => localizedMessageAddToParty.GetSafeLocalizedString();
        
        public override List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateMachine, CheckWithConfiguration callingCheck)
        {
            Party party = playerStateMachine.GetParty();
            return party.GetAvailableCharactersToAdd().Select(character => 
                new ChoiceActionPair(CharacterProperties.GetCharacterDisplayName(character), () => AddToPartyWithErrorHandling(playerStateMachine, party, character))).ToList();
        }

        public override List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedMessageAddToParty.TableEntryReference,
                localizedMessagePartyFull.TableEntryReference
            };
        }
        
        private void AddToPartyWithErrorHandling(PlayerStateMachine playerStateMachine, Party party, CharacterProperties characterProperties)
        {
            if (!party.AddToParty(characterProperties))
            {
                playerStateMachine.EnterDialogue(localizedMessagePartyFull.GetSafeLocalizedString());
            }
        }
    }
}
