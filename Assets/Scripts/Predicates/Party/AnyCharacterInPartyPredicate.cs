using System.Linq;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Core.Predicates
{
    [CreateAssetMenu(fileName = "New Any Character In Party Predicate", menuName = "Predicates/Party/ContainsAnyCharacter")]
    public class AnyCharacterInPartyPredicate : PredicateParty
    {
        public override bool? Evaluate(Party party)
        {
            if (charactersToMatch.Count == 0) { return false; }

            foreach (BaseStats baseStats in party.GetParty())
            {
                if (baseStats == null) { continue; }
                if (charactersToMatch.Any(characterToMatch => CharacterProperties.AreCharacterPropertiesMatched(characterToMatch, baseStats.GetCharacterProperties())))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
