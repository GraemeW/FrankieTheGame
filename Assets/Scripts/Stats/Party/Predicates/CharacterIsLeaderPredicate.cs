using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Stats
{
    [CreateAssetMenu(fileName = "New Character Is Leader Predicate", menuName = "Predicates/Party/Is Character Party Leader", order = 5)]
    public class CharacterIsLeaderPredicate : PredicateParty
    {
        [SerializeField] private PartyBehaviourType partyBehaviourType = PartyBehaviourType.Party;
        [SerializeField] private List<CharacterProperties> charactersToMatch = new();
        
        public override bool? Evaluate(PartyBehaviour partyBehaviour)
        {
            if (!PartyBehaviour.IsPartyBehaviourType(partyBehaviour, partyBehaviourType)) { return null; }
            if (charactersToMatch.Count == 0) { return false; }
            return partyBehaviour.TryGetPartyLeader(out BaseStats partyLeader) && charactersToMatch.Any(characterToMatch => CharacterProperties.AreCharacterPropertiesMatched(characterToMatch, partyLeader.GetCharacterProperties()));
        }
    }
}
