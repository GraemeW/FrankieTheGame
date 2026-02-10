using UnityEngine;
using Frankie.Combat;

namespace Frankie.Core
{
    [CreateAssetMenu(fileName = "New Is Anyone Dead Predicate", menuName = "Predicates/CombatParticipant/Is Anyone Dead")]
    public class IsAnyoneDeadPredicate : PredicateCombatParticipant
    {
        public override bool? Evaluate(CombatParticipant combatParticipant)
        {
            if (combatParticipant == null) { return null; }
            if (combatParticipant.IsDead()) { return true; }
            return null;
        }
    }
}
