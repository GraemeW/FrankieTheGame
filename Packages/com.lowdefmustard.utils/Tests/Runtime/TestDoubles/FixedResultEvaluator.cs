namespace LowDefMustard.Utils.Tests
{
    // Always returns a fixed result regardless of which predicate queried
    public sealed class FixedResultEvaluator : IPredicateEvaluator
    {
        private readonly bool? result;
        public FixedResultEvaluator(bool? result) => this.result = result;
        public bool? Evaluate(Predicate predicate) => result;
    }
}
