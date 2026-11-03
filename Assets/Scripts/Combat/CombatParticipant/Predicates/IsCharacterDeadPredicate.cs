using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Combat
{
    [CreateAssetMenu(fileName = "New Is Character Dead Predicate", menuName = "Predicates/CombatParticipant/Is Character Dead", order = 5)]
    public class IsCharacterDeadPredicate : PredicateCombatParticipant
    {
        [SerializeField] private List<CharacterProperties> characters = new();
        
        public override bool? Evaluate(CombatParticipant combatParticipant)
        {
            if (combatParticipant == null) { return null; }
            var baseStats = combatParticipant.GetComponent<BaseStats>();
            if (characters.Any(characterToMatch => CharacterProperties.AreCharacterPropertiesMatched(characterToMatch, baseStats.GetCharacterProperties())))
            {
                return combatParticipant.IsDead();
            }
            return null;
        }
    }
}
