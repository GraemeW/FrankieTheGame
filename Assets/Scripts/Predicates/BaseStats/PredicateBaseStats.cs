using UnityEngine;
using Frankie.Stats;

namespace Frankie.Core.Predicates
{
    public abstract class PredicateBaseStats : Predicate
    {
        [SerializeField] [Tooltip("Optional, depending on implementation")] protected CharacterProperties character;
        public abstract bool? Evaluate(BaseStats baseStats);
    }
}
