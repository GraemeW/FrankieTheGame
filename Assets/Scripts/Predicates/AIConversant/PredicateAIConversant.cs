using Frankie.Speech;

namespace Frankie.Core.Predicates
{
    public abstract class PredicateAIConversant : Predicate
    {
        public abstract bool? Evaluate(AIConversant aiConversant);
    }
}
