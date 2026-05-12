using UnityEngine;
using Frankie.Quests;

namespace Frankie.Core.Predicates
{
    [CreateAssetMenu(fileName = "New Has Quest Predicate", menuName = "Predicates/QuestList/Has Quest", order = 5)]
    public class HasQuestPredicate : PredicateQuestList
    {
        public override bool? Evaluate(QuestList questList)
        {
            if (questList == null) { return null; }
            return quest != null && questList.HasQuest(quest);
        }
    }
}
