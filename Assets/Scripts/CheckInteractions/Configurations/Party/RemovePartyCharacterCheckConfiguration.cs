using Frankie.Combat;
using Frankie.Stats;
using Frankie.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control
{
    [CreateAssetMenu(fileName = "New Remove from Party Check Configuration", menuName = "CheckConfigurations/RemoveParty")]
    public class RemovePartyCharacterCheckConfiguration : CheckConfiguration
    {
        [SerializeField] string messageRemoveFromParty = "Who you want to abandon?";
        [SerializeField][Tooltip("Use {0} for character name")] string messageCannotRemove = "{0} can't be removed from the party at this time";
        [SerializeField] List<CharacterProperties> unremovableCharacters = new List<CharacterProperties>();
        [SerializeField] string messageMinimumParty = "Er, what is consciousness without a vessel in which it can exist?";

        public override List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateHandler, CheckWithConfiguration callingCheck)
        {
            Party party = playerStateHandler.GetParty();

            List<ChoiceActionPair> interactActions = new List<ChoiceActionPair>();
            if (party.GetPartySize() == 1) { return interactActions; } // throw empty list to prevent option from triggering

            foreach (BaseStats character in party.GetParty())
            {
                interactActions.Add(new ChoiceActionPair(character.GetCharacterProperties().GetCharacterNamePretty(),
                    () => RemoveFromPartyWithErrorHandling(playerStateHandler, party, character)));
            }
            return interactActions;
        }

        public override string GetMessage()
        {
            return messageRemoveFromParty;
        }

        private void RemoveFromPartyWithErrorHandling(PlayerStateMachine playerStateHandler, Party party, BaseStats character)
        {
            if (unremovableCharacters != null)
            {
                CharacterProperties selectedCharacter = character.GetCharacterProperties();
                foreach(CharacterProperties unremovableCharacter in unremovableCharacters)
                {
                    // Check via name comparison for compatibility with addressables system
                    if (selectedCharacter.GetCharacterNameID() == unremovableCharacter.GetCharacterNameID())
                    {
                        playerStateHandler.EnterDialogue(string.Format(messageCannotRemove, selectedCharacter.name));
                        return;
                    }
                }
            }

            if (!party.RemoveFromParty(character))
            {
                playerStateHandler.EnterDialogue(messageMinimumParty);
            }
        }
    }
}