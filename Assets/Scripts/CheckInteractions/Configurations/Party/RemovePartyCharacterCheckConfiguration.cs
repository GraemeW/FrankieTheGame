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
    [CreateAssetMenu(fileName = "New Remove from Party Check Configuration", menuName = "CheckConfigurations/Party/RemoveFromParty", order = 5)]
    public class RemovePartyCharacterCheckConfiguration : CheckConfiguration
    {
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageRemoveFromParty;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageCannotRemove;
        [SerializeField] private List<CharacterProperties> unremovableCharacters = new();
        [SerializeField][SimpleLocalizedString(LocalizationTableType.ChecksWorldObjects, true)] private LocalizedString localizedMessageMinimumParty;

        public override string GetMessage() => localizedMessageRemoveFromParty.GetSafeLocalizedString();
        
        public override List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateMachine, CheckWithConfiguration callingCheck)
        {
            Party party = playerStateMachine.GetParty();

            var interactActions = new List<ChoiceActionPair>();
            if (party.GetPartySize() == 1) { return interactActions; } // throw empty list to prevent option from triggering

            interactActions.AddRange(party.GetMembers().Select(character => 
                new ChoiceActionPair(CharacterProperties.GetCharacterDisplayName(character), () => RemoveFromPartyWithErrorHandling(playerStateMachine, party, character))));
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

        private void RemoveFromPartyWithErrorHandling(PlayerStateMachine playerStateMachine, Party party, BaseStats character)
        {
            if (unremovableCharacters != null)
            {
                CharacterProperties selectedCharacter = character.GetCharacterProperties();
                if (unremovableCharacters.Any(unremovableCharacter => CharacterProperties.AreCharacterPropertiesMatched(selectedCharacter, unremovableCharacter)))
                {
                    playerStateMachine.EnterDialogue(string.Format(localizedMessageCannotRemove.GetSafeLocalizedString(), selectedCharacter.GetCharacterID()));
                    return;
                }
            }

            if (!party.RemoveFromParty(character))
            {
                playerStateMachine.EnterDialogue(localizedMessageMinimumParty.GetSafeLocalizedString());
            }
        }
    }
}
