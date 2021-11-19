using Frankie.Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    [CreateAssetMenu(fileName = "New Is Anyone Dead Predicate", menuName = "Predicates/CombatParticipant/Is Anyone Dead")]
    public class IsAnyoneDeadPredicate : PredicateCombatParticipant
    {
        public override bool? Evaluate(CombatParticipant combatParticipant)
        {
            if (combatParticipant.IsDead())
            {
                return true;
            }
            return null;
        }
    }
}
