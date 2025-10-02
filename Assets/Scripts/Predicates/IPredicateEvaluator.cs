namespace Frankie.Core
{
    public interface IPredicateEvaluator
    {
        bool? Evaluate(Predicate predicate);
    }
}
