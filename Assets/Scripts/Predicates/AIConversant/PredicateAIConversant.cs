using Frankie.Speech;

namespace Frankie.Core
{
    public abstract class PredicateAIConversant : Predicate
    {
        public abstract bool? Evaluate(AIConversant aiConversant);
    }
}
