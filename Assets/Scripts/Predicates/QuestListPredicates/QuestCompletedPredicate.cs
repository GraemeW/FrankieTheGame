using UnityEngine;
using Frankie.Quests;

namespace Frankie.Core
{
    [CreateAssetMenu(fileName = "New Quest Completed Predicate", menuName = "Predicates/QuestList/Quest Completed")]
    public class QuestCompletedPredicate : PredicateQuestList
    {
        public override bool? Evaluate(QuestList questList)
        {
            QuestStatus questStatus = questList.GetQuestStatus(quest);
            return questStatus != null && // i.e. haven't even started the quest, let alone completed
                   questStatus.IsComplete();
        }
    }
}
