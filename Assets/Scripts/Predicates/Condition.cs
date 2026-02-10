using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Core
{
    [System.Serializable]
    public class Condition
    {
        [NonReorderable] [SerializeField] private List<Disjunction> and = new();

        public bool Check(IEnumerable<IPredicateEvaluator> evaluators)
        {
            return and.All(disjunction => disjunction.Check(evaluators));
        }

        [System.Serializable]
        private class Disjunction
        {
            [NonReorderable] [SerializeField] private List<PredicateWrapper> or = new();

            public bool Check(IEnumerable<IPredicateEvaluator> evaluators)
            {
                return or.Any(predicateWrapper => predicateWrapper.Check(evaluators));
            }
        }

        [System.Serializable]
        private class PredicateWrapper
        {
            [SerializeField] private Predicate predicate;
            [SerializeField] private bool negate;

            public bool Check(IEnumerable<IPredicateEvaluator> evaluators)
            {
                if (predicate == null) { return true; }
                return evaluators.Select(evaluator => evaluator.Evaluate(predicate)).All(result => result != negate);
            }
        }
    }
}
