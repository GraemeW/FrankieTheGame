using System.Linq;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Core.Predicates
{
    [CreateAssetMenu(fileName = "New Character Is Leader Predicate", menuName = "Predicates/Party/IsLeader")]
    public class CharacterIsLeaderPredicate : PredicateParty
    {
        public override bool? Evaluate(Party party)
        {
            if (charactersToMatch.Count == 0) { return false; }

            BaseStats leader = party.GetPartyLeader();
            if (leader == null) { return false; }
            CharacterProperties leaderCharacterProperties = leader.GetCharacterProperties();
            if (leaderCharacterProperties == null) { return false; }
            
            return charactersToMatch.Any(character => character.GetCharacterNameID() == leaderCharacterProperties.GetCharacterNameID());
        }
    }
}
