using UnityEngine;
using Frankie.Quests;

namespace Frankie.Core
{
    public abstract class PredicateQuestList : Predicate
    {
        [SerializeField] [Tooltip("Optional, depending on implementation")] protected Quest quest;
        [SerializeField] [Tooltip("Optional, depending on implementation")] protected QuestObjective objective;

        public abstract bool? Evaluate(QuestList questList);
    }
}
