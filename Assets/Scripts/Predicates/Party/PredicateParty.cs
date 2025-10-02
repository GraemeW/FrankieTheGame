using UnityEngine;
using Frankie.Stats;

namespace Frankie.Core
{
    public abstract class PredicateParty : Predicate
    {
        [Tooltip("Optional, depending on implementation")] [SerializeField] protected CharacterProperties[] charactersToMatch = null;

        public abstract bool? Evaluate(Party party);
    }
}
