using Frankie.Quests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    [CreateAssetMenu(fileName = "New Quest Objective Completed Predicate", menuName = "Predicates/QuestList/Quest Objective Completed")]
    public class QuestObjectiveCompletedPredicate : PredicateQuestList
    {
        public override bool? Evaluate(QuestList questList)
        {

            QuestStatus questStatus = questList.GetQuestStatus(quest);
            if (questStatus == null) { return false; }
            bool objectiveStatus = questStatus.GetStatusForObjective(objective);
            return objectiveStatus;
        }
    }
}
