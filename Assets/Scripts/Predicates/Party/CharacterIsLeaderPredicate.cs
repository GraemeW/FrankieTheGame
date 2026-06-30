using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Core.Predicates
{
    [CreateAssetMenu(fileName = "New Character Is Leader Predicate", menuName = "Predicates/Party/Is Character Party Leader", order = 5)]
    public class CharacterIsLeaderPredicate : PredicateParty
    {
        [SerializeField] private List<CharacterProperties> charactersToMatch = new();
        
        public override bool? Evaluate(Party party)
        {
            if (charactersToMatch.Count == 0) { return false; }
            BaseStats leader = party.GetPartyLeader();
            return leader != null && charactersToMatch.Any(characterToMatch => CharacterProperties.AreCharacterPropertiesMatched(characterToMatch, leader.GetCharacterProperties()));
        }
    }
}
