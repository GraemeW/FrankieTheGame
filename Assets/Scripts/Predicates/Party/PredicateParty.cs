using Frankie.Stats;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    public abstract class PredicateParty : Predicate
    {
        [Tooltip("Optional, depending on implementation")] [SerializeField] protected CharacterProperties[] charactersToMatch = null;

        public abstract bool? Evaluate(Party party);
    }
}
