using UnityEngine;
using Frankie.Quests;

namespace Frankie.Core
{
    [CreateAssetMenu(fileName = "New Has Quest Predicate", menuName = "Predicates/QuestList/Has Quest")]
    public class HasQuestPredicate : PredicateQuestList
    {
        public override bool? Evaluate(QuestList questList)
        {
            return questList.HasQuest(quest);
        }
    }
}
