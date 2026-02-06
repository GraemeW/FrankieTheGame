using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frankie.Core;

namespace Frankie.Combat
{
    [System.Serializable]
    public class BattleAICondition
    {
        [NonReorderable] [SerializeField] private Disjunction[] and;
        public bool Check(IEnumerable<IPredicateEvaluator> evaluators)
        {
            // logical 'AND' implementation for Conjunction
            return and.All(disjunction => disjunction.Check(evaluators));
        }

        [System.Serializable]
        private class Disjunction
        {
            [NonReorderable] [SerializeField] private PredicateWrapper[] or;
            public bool Check(IEnumerable<IPredicateEvaluator> evaluators)
            {
                // logical 'OR' implementation for Disjunction
                return or.Any(predicateWrapper => predicateWrapper.Check(evaluators));
            }
        }

        [System.Serializable]
        private class PredicateWrapper
        {
            [SerializeField] private BattleAIPredicate predicate;
            [SerializeField] private bool negate;

            public bool Check(IEnumerable<IPredicateEvaluator> evaluators)
            {
                if (predicate == null) { return true; }
                return evaluators.Select(evaluator => evaluator.Evaluate(predicate)).All(result => result != negate);
            }
        }
    }
}
