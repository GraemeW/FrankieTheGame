using LowDefMustard.Utils;

namespace Frankie.Stats
{
    public abstract class PredicateBaseStats : Predicate
    {
        public abstract bool? Evaluate(BaseStats baseStats);
    }
}
