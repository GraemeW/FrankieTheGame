using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Core.Predicates
{
    [CreateAssetMenu(fileName = "New Any Character In Party Predicate", menuName = "Predicates/Party/Contains Any Character", order = 5)]
    public class AnyCharacterInPartyPredicate : PredicateParty
    {
        [SerializeField] private List<CharacterProperties> charactersToMatch = new();
        
        public override bool? Evaluate(Party party)
        {
            if (charactersToMatch.Count == 0) { return false; }

            foreach (BaseStats baseStats in party.GetMembers())
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
