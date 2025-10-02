using UnityEngine;
using Frankie.Stats;

namespace Frankie.Core
{
    public abstract class PredicateBaseStats : Predicate
    {
        [SerializeField] [Tooltip("Optional, depending on implementation")] protected CharacterProperties character = null;
        public abstract bool? Evaluate(BaseStats baseStats);
    }
}
