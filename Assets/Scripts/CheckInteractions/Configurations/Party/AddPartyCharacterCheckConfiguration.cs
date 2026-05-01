using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Stats;
using Frankie.Utils;
using Frankie.Utils.Localization;

namespace Frankie.Control
{
    [CreateAssetMenu(fileName = "New Add to Party Check Configuration", menuName = "CheckConfigurations/AddParty")]
    public class AddPartyCharacterCheckConfiguration : CheckConfiguration
    {
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageAddToParty;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessagePartyFull;

        public override string GetMessage() => localizedMessageAddToParty.GetSafeLocalizedString();
        
        public override List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateHandler, CheckWithConfiguration callingCheck)
        {
            Party party = playerStateHandler.GetParty();
            return party.GetAvailableCharactersToAdd().Select(character => 
                new ChoiceActionPair(character.GetCharacterNamePretty(), () => AddToPartyWithErrorHandling(playerStateHandler, party, character))).ToList();
        }

        public override List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedMessageAddToParty.TableEntryReference,
                localizedMessagePartyFull.TableEntryReference
            };
        }
        
        private void AddToPartyWithErrorHandling(PlayerStateMachine playerStateHandler, Party party, CharacterProperties characterProperties)
        {
            if (!party.AddToParty(characterProperties))
            {
                playerStateHandler.EnterDialogue(localizedMessagePartyFull.GetSafeLocalizedString());
            }
        }
    }
}
