using Frankie.Core.Predicates;

namespace Frankie.Combat
{
    public abstract class BattleAIPredicate : Predicate
    {
        public abstract bool? Evaluate(BattleAI battleAI);
    }
}
