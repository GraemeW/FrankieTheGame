using UnityEngine;

namespace LowDefMustard.Utils.Tests
{
    public sealed class FixedResultEvaluatorBehaviour : MonoBehaviour, IPredicateEvaluator
    {
        // Component form of FixedResultEvaluator ~ use w/ code that finds evaluators through GetComponents / GetComponentsInChildren
        public bool? result;
        public bool? Evaluate(Predicate predicate) => result;
    }
}
