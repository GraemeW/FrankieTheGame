using UnityEngine;
using Frankie.Quests;

namespace Frankie.Core
{
    [CreateAssetMenu(fileName = "New Quest Completed Predicate", menuName = "Predicates/QuestList/Quest Completed")]
    public class QuestCompletedPredicate : PredicateQuestList
    {
        public override bool? Evaluate(QuestList questList)
        {
            if (questList == null) { return null; }
            if (quest == null) { return false; }
            
            QuestStatus questStatus = questList.GetQuestStatus(quest);
            return questStatus != null && questStatus.IsComplete();
        }
    }
}
