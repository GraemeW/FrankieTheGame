using System.Collections.Generic;
using System.Linq;

namespace LowDefMustard.Utils.Tests
{
    public static class EvaluatorBuilder
    {
        // Shorthand test convenience evaluator builder
        //  - Evaluators: one PerPredicateEvaluator answering each listed predicate (unlisted predicates abstain)
        
        public static List<IPredicateEvaluator> Evaluators(params (Predicate predicate, bool? result)[] results)
        {
            return new List<IPredicateEvaluator> { new PerPredicateEvaluator(results.ToDictionary(entry => entry.predicate, entry => entry.result)) };
        }
    }
}
