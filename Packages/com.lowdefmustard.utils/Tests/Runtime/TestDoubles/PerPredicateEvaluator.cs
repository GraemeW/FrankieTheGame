using System.Collections.Generic;

namespace LowDefMustard.Utils.Tests
{
    public sealed class PerPredicateEvaluator : IPredicateEvaluator
    {
        // Answers per predicate ~ unknown predicates abstain (null)
        private readonly Dictionary<Predicate, bool?> results;
        public PerPredicateEvaluator(Dictionary<Predicate, bool?> results) => this.results = results;
        public bool? Evaluate(Predicate predicate) => results.GetValueOrDefault(predicate);
    }
}
