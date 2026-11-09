namespace LowDefMustard.Utils.Tests
{
    public sealed class FixedResultEvaluator : IPredicateEvaluator
    {
        // Always returns a fixed result regardless of which predicate queried
        private readonly bool? result;
        public FixedResultEvaluator(bool? result) => this.result = result;
        public bool? Evaluate(Predicate predicate) => result;
    }
}
