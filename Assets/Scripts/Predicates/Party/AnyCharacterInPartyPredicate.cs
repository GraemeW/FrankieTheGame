using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    [CreateAssetMenu(fileName = "New Any Character In Party Predicate", menuName = "Predicates/Party/ContainsAnyCharacter")]
    public class AnyCharacterInPartyPredicate : PredicateParty
    {
        public override bool? Evaluate(Party party)
        {
            if (charactersToMatch == null || charactersToMatch.Length == 0) { return false; }

            foreach (BaseStats character in party.GetParty())
            {
                CharacterProperties characterProperties = character.GetCharacterProperties();
                if (characterProperties == null) { continue; }

                foreach (CharacterProperties characterToMatch in charactersToMatch)
                {
                    if (characterProperties.GetCharacterNameID() == characterToMatch.GetCharacterNameID())
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}