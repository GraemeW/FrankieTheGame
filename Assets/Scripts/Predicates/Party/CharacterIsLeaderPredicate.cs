using Frankie.Combat;
using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    [CreateAssetMenu(fileName = "New Character Is Leader Predicate", menuName = "Predicates/Party/IsLeader")]
    public class CharacterIsLeaderPredicate : PredicateParty
    {
        public override bool? Evaluate(Party party)
        {
            if (charactersToMatch == null || charactersToMatch.Length == 0) { return false; }

            CombatParticipant leader = party.GetPartyLeader();
            CharacterProperties leaderCharacterProperties = leader?.GetBaseStats()?.GetCharacterProperties();
            if (leaderCharacterProperties == null) { return false; }

            foreach (CharacterProperties character in charactersToMatch)
            {
                if (character.GetCharacterNameID() == leaderCharacterProperties.GetCharacterNameID()) { return true; }
            }
            return false;
        }
    }
}