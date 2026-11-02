using LowDefMustard.Utils;

namespace Frankie.Quests
{
    public abstract class PredicateQuestList : Predicate
    {
        public abstract bool? Evaluate(QuestList questList);
    }
}
