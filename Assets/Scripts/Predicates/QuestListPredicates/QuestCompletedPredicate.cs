using Frankie.Quests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    public class QuestCompletedPredicate : PredicateQuestList
    {
        public override bool? Evaluate(QuestList questList)
        {
            QuestStatus questStatus = questList.GetQuestStatus(quest);
            if (questStatus == null) { return false; }
            bool objectiveStatus = questStatus.GetStatusForObjectiveID(objective);
            return objectiveStatus;
        }
    }
}