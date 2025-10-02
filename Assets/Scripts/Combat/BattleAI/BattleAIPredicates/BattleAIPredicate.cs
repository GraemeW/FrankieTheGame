using Frankie.Core;

namespace Frankie.Combat
{
    public abstract class BattleAIPredicate : Predicate
    {
        public abstract bool? Evaluate(BattleAI battleAI);
    }
}
