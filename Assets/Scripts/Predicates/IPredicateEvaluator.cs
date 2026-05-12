namespace Frankie.Core.Predicates
{
    public interface IPredicateEvaluator
    {
        bool? Evaluate(Predicate predicate);
    }
}
