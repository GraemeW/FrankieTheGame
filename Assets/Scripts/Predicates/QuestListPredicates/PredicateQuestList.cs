using Frankie.Quests;

namespace Frankie.Core.Predicates
{
    public abstract class PredicateQuestList : Predicate
    {
        public abstract bool? Evaluate(QuestList questList);
    }
}
