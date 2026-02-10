using System.Linq;
using UnityEngine;
using Frankie.Combat;
using Frankie.Stats;

namespace Frankie.Core
{
    [CreateAssetMenu(fileName = "New Is Character Dead Predicate", menuName = "Predicates/CombatParticipant/Is Character Dead")]
    public class IsCharacterDeadPredicate : PredicateCombatParticipant
    {
        public override bool? Evaluate(CombatParticipant combatParticipant)
        {
            if (combatParticipant == null) { return null; }
            var baseStats = combatParticipant.GetComponent<BaseStats>();
            CharacterProperties characterProperties = baseStats.GetCharacterProperties();

            if (characters.Any(characterPropertiesToCompare => characterProperties.GetCharacterNameID() == characterPropertiesToCompare.GetCharacterNameID()))
            {
                return combatParticipant.IsDead();
            }
            return null;
        }
    }
}
