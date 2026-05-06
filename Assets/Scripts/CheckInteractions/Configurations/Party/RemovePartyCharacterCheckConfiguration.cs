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
    [CreateAssetMenu(fileName = "New Remove from Party Check Configuration", menuName = "CheckConfigurations/Party/RemoveFromParty", order = 5)]
    public class RemovePartyCharacterCheckConfiguration : CheckConfiguration
    {
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageRemoveFromParty;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageCannotRemove;
        [SerializeField] private List<CharacterProperties> unremovableCharacters = new();
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageMinimumParty;

        public override string GetMessage() => localizedMessageRemoveFromParty.GetSafeLocalizedString();
        
        public override List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateHandler, CheckWithConfiguration callingCheck)
        {
            Party party = playerStateHandler.GetParty();

            var interactActions = new List<ChoiceActionPair>();
            if (party.GetPartySize() == 1) { return interactActions; } // throw empty list to prevent option from triggering

            interactActions.AddRange(party.GetParty().Select(character => 
                new ChoiceActionPair(character.GetCharacterProperties().GetCharacterDisplayName(), () => RemoveFromPartyWithErrorHandling(playerStateHandler, party, character))));
            return interactActions;
        }
        
        public override List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedMessageRemoveFromParty.TableEntryReference,
                localizedMessageCannotRemove.TableEntryReference,
                localizedMessageMinimumParty.TableEntryReference
            };
        }

        private void RemoveFromPartyWithErrorHandling(PlayerStateMachine playerStateHandler, Party party, BaseStats character)
        {
            if (unremovableCharacters != null)
            {
                CharacterProperties selectedCharacter = character.GetCharacterProperties();
                if (unremovableCharacters.Any(unremovableCharacter => CharacterProperties.AreCharacterPropertiesMatched(selectedCharacter, unremovableCharacter)))
                {
                    playerStateHandler.EnterDialogue(string.Format(localizedMessageCannotRemove.GetSafeLocalizedString(), selectedCharacter.name));
                    return;
                }
            }

            if (!party.RemoveFromParty(character))
            {
                playerStateHandler.EnterDialogue(localizedMessageMinimumParty.GetSafeLocalizedString());
            }
        }
    }
}
