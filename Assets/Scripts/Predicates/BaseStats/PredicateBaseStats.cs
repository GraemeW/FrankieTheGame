using LowDefMustard.Utils;
using Frankie.Stats;

namespace Frankie.Core.Predicates
{
    public abstract class PredicateBaseStats : Predicate
    {
        public abstract bool? Evaluate(BaseStats baseStats);
    }
}
