using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Core
{
    public abstract class PredicateBaseStats : Predicate
    {
        [SerializeField] [Tooltip("Optional, depending on implementation")] protected CharacterProperties character = null;
        [SerializeField] [Tooltip("Optional, depending on implementation")] protected Stat stat = default;
        [SerializeField] [Tooltip("Optional, depending on implementation")] protected float value = 0f;
        public abstract bool? Evaluate(BaseStats baseStats);
    }
}
