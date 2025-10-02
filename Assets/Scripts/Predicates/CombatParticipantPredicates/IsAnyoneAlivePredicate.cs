using UnityEngine;
using Frankie.Combat;

namespace Frankie.Core
{
    [CreateAssetMenu(fileName = "New Is Anyone Alive Predicate", menuName = "Predicates/CombatParticipant/Is Anyone Alive")]
    public class IsAnyoneAlivePredicate : PredicateCombatParticipant
    {
        public override bool? Evaluate(CombatParticipant combatParticipant)
        {
            if (!combatParticipant.IsDead())
            {
                return true;
            }
            return null;
        }
    }
}
