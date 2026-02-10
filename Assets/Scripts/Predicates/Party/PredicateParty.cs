using System.Collections.Generic;
using UnityEngine;
using Frankie.Stats;

namespace Frankie.Core
{
    public abstract class PredicateParty : Predicate
    {
        [Tooltip("Optional, depending on implementation")] [SerializeField] protected List<CharacterProperties> charactersToMatch = new();
        public abstract bool? Evaluate(Party party);
    }
}
