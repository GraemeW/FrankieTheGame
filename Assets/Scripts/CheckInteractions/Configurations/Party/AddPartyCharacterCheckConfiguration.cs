using System.Collections.Generic;
using UnityEngine;
using Frankie.Stats;
using Frankie.Utils;

namespace Frankie.Control
{
    [CreateAssetMenu(fileName = "New Add to Party Check Configuration", menuName = "CheckConfigurations/AddParty")]
    public class AddPartyCharacterCheckConfiguration : CheckConfiguration
    {
        [SerializeField] private string messageAddToParty = "Who do you want to tag along?";
        [SerializeField] private string messagePartyFull = "Hey Neve, this isn't a party of five";

        public override string GetMessage() => messageAddToParty;
        
        public override List<ChoiceActionPair> GetChoiceActionPairs(PlayerStateMachine playerStateHandler, CheckWithConfiguration callingCheck)
        {
            Party party = playerStateHandler.GetParty();

            var interactActions = new List<ChoiceActionPair>();
            foreach (CharacterProperties character in party.GetAvailableCharactersToAdd())
            {
                interactActions.Add(new ChoiceActionPair(character.GetCharacterNamePretty(),
                    () => AddToPartyWithErrorHandling(playerStateHandler, party, character)));
            }
            return interactActions;
        }

        private void AddToPartyWithErrorHandling(PlayerStateMachine playerStateHandler, Party party, CharacterProperties characterProperties)
        {
            if (!party.AddToParty(characterProperties))
            {
                playerStateHandler.EnterDialogue(messagePartyFull);
            }
        }
    }
}
