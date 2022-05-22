using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    [System.Serializable]
    public class Condition
    {
        [NonReorderable][SerializeField]
        Disjunction[] and;

        public bool Check(IEnumerable<IPredicateEvaluator> evaluators)
        {
            foreach (Disjunction disjunction in and) // logical 'AND' implementation for Conjunction
            {
                if (!disjunction.Check(evaluators))
                {
                    return false;
                }
            }
            return true;
        }

        [System.Serializable]
        class Disjunction
        {
            [NonReorderable][SerializeField]
            PredicateWrapper[] or;

            public bool Check(IEnumerable<IPredicateEvaluator> evaluators)
            {
                foreach (PredicateWrapper predicateWrapper in or) // logical 'OR' implementation for Disjunction
                {
                    if (predicateWrapper.Check(evaluators))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        [System.Serializable]
        class PredicateWrapper
        {
            [SerializeField] Predicate predicate;
            [SerializeField] bool negate;

            public bool Check(IEnumerable<IPredicateEvaluator> evaluators)
            {
                if (predicate == null) { return true; }

                foreach (IPredicateEvaluator evaluator in evaluators)
                {
                    bool? result = evaluator.Evaluate(predicate);

                    if (result == null) { continue; }
                    if (result == negate) { return false; }
                }
                return true;
            }
        }
    }
}