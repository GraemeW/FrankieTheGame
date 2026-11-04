using System.Collections.Generic;

namespace LowDefMustard.Utils.Tests
{
    // Answers per predicate
    // Unknown predicates abstain (null)
    public sealed class PerPredicateEvaluator : IPredicateEvaluator
    {
        private readonly Dictionary<Predicate, bool?> results;
        public PerPredicateEvaluator(Dictionary<Predicate, bool?> results) => this.results = results;
        public bool? Evaluate(Predicate predicate) => results.GetValueOrDefault(predicate);
    }
}
