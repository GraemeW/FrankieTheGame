using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    public static class PredicateEvaluatorExtension
    {
        public static string MatchToPredicates(this IPredicateEvaluator predicateEvaluator, string predicate, string[] PREDICATES_ARRAY)
        {
            string matchingPredicate = null;
            foreach (string AVAILABLE_PREDICATE in PREDICATES_ARRAY)
            {
                if (predicate == AVAILABLE_PREDICATE) { matchingPredicate = predicate; }
            }
            return matchingPredicate;
        }
    }
}