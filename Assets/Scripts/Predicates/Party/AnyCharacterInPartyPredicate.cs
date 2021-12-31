using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Combat;

namespace Frankie.Core
{
    [CreateAssetMenu(fileName = "New Any Character In Party Predicate", menuName = "Predicates/Party/ContainsAnyCharacter")]
    public class AnyCharacterInPartyPredicate : PredicateParty
    {
        public override bool? Evaluate(Party party)
        {
            if (charactersToMatch == null || charactersToMatch.Length == 0) { return false; }

            List<CharacterProperties> partyCharacterProperties = new List<CharacterProperties>();
            foreach (CombatParticipant character in party.GetParty())
            {
                CharacterProperties characterProperties = character.GetBaseStats()?.GetCharacterProperties();
                if (characterProperties == null) { continue; }

                partyCharacterProperties.Add(characterProperties);
            }

            foreach (CharacterProperties characterToMatch in charactersToMatch)
            {
                if (partyCharacterProperties.Contains(characterToMatch))
                {
                    return true;
                }
            }
            return false;
        }
    }
}