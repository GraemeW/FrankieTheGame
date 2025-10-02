using UnityEngine;
using Frankie.Combat;
using Frankie.Stats;

namespace Frankie.Core
{
    public abstract class PredicateCombatParticipant : Predicate
    {
        [SerializeField][Tooltip("Optional, depending on implementation")] protected CharacterProperties[] characters = null;
        public abstract bool? Evaluate(CombatParticipant combatParticipant);
    }
}
