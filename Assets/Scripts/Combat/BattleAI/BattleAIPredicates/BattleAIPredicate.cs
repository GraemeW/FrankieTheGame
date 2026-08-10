using LowDefMustard.Utils;

namespace Frankie.Combat
{
    public abstract class BattleAIPredicate : Predicate
    {
        public abstract bool? Evaluate(BattleAI battleAI);
    }
}
