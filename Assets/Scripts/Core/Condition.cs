using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    [System.Serializable]
    public class Condition
    {
        [SerializeField]
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
            [SerializeField]
            Predicate[] or;

            public bool Check(IEnumerable<IPredicateEvaluator> evaluators)
            {
                foreach (Predicate predicate in or) // logical 'OR' implementation for Disjunction
                {
                    if (predicate.Check(evaluators))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        [System.Serializable]
        class Predicate
        {
            [SerializeField] string predicate;
            [SerializeField] bool negate;
            [SerializeField] string[] parameters;

            public bool Check(IEnumerable<IPredicateEvaluator> evaluators)
            {
                if (string.IsNullOrWhiteSpace(predicate)) { return true; }

                foreach (IPredicateEvaluator evaluator in evaluators)
                {
                    bool? result = evaluator.Evaluate(predicate, parameters);

                    if (result == null) { continue; }
                    if (result == negate) { return false; }
                }
                return true;
            }
        }
    }
}