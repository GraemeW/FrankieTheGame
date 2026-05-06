using System.Linq;
using UnityEngine;
using Frankie.Combat;
using Frankie.Stats;

namespace Frankie.Core.Predicates
{
    [CreateAssetMenu(fileName = "New Is Character Dead Predicate", menuName = "Predicates/CombatParticipant/Is Character Dead", order = 5)]
    public class IsCharacterDeadPredicate : PredicateCombatParticipant
    {
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
