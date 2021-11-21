using Frankie.Quests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    [CreateAssetMenu(fileName = "New Has Quest Predicate", menuName = "Predicates/QuestList/Has Quest")]
    public class HasQuestPredicate : PredicateQuestList
    {
        public override bool? Evaluate(QuestList questList)
        {
            if (questList.HasQuest(quest))
            {
                return true;
            }
            return false;
        }
    }
}
