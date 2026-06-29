using UnityEngine;
using Frankie.Quests;

namespace Frankie.Core.Predicates
{
    [CreateAssetMenu(fileName = "New Quest Objective Completed Predicate", menuName = "Predicates/QuestList/Quest Objective Completed", order = 5)]
    public class QuestObjectiveCompletedPredicate : PredicateQuestList
    {
        [SerializeField] private QuestObjective objective;
        
        public override bool? Evaluate(QuestList questList)
        {
            if (questList == null) { return null; }
            if (objective == null) { return false; }

            Quest quest = Quest.GetFromID(objective.GetQuestID());
            if (quest == null) { return false; }

            QuestStatus questStatus = questList.GetQuestStatus(quest);
            return questStatus != null && questStatus.GetStatusForObjective(objective);
        }
    }
}
