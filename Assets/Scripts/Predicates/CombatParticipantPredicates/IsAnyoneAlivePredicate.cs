using UnityEngine;
using Frankie.Combat;

namespace Frankie.Core.Predicates
{
    [CreateAssetMenu(fileName = "New Is Anyone Alive Predicate", menuName = "Predicates/CombatParticipant/Is Anyone Alive", order = 5)]
    public class IsAnyoneAlivePredicate : PredicateCombatParticipant
    {
        public override bool? Evaluate(CombatParticipant combatParticipant)
        {
            if (combatParticipant == null) { return null; }
            if (!combatParticipant.IsDead()) { return true; }
            return null;
        }
    }
}
