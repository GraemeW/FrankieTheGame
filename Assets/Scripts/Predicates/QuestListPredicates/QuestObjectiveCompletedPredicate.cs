using UnityEngine;
using Frankie.Quests;

namespace Frankie.Core
{
    [CreateAssetMenu(fileName = "New Quest Objective Completed Predicate", menuName = "Predicates/QuestList/Quest Objective Completed")]
    public class QuestObjectiveCompletedPredicate : PredicateQuestList
    {
        public override bool? Evaluate(QuestList questList)
        {
            if (questList == null) { return null; }
            if (quest == null || objective == null) { return false; }

            QuestStatus questStatus = questList.GetQuestStatus(quest);
            return questStatus != null && questStatus.GetStatusForObjective(objective);
        }
    }
}
