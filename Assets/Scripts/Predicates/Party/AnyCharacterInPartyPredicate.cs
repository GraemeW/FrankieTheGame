using System.Linq;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Core
{
    [CreateAssetMenu(fileName = "New Any Character In Party Predicate", menuName = "Predicates/Party/ContainsAnyCharacter")]
    public class AnyCharacterInPartyPredicate : PredicateParty
    {
        public override bool? Evaluate(Party party)
        {
            if (charactersToMatch.Count == 0) { return false; }

            foreach (BaseStats character in party.GetParty())
            {
                CharacterProperties characterProperties = character.GetCharacterProperties();
                if (characterProperties == null) { continue; }

                if (charactersToMatch.Any(characterToMatch => characterProperties.GetCharacterNameID() == characterToMatch.GetCharacterNameID()))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
