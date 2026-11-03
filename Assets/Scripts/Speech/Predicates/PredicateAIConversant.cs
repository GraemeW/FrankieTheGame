using LowDefMustard.Utils;

namespace Frankie.Speech
{
    public abstract class PredicateAIConversant : Predicate
    {
        public abstract bool? Evaluate(AIConversant aiConversant);
    }
}
