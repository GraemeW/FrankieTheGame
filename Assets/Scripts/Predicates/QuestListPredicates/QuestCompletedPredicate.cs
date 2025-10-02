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
            if (questStatus == null) { return false; } // i.e. haven't even started the quest, let alone completed

            if (questStatus.IsComplete())
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
