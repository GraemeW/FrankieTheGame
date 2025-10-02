using UnityEngine;
using Frankie.Quests;

namespace Frankie.Core
{
    public abstract class PredicateQuestList : Predicate
    {
        [SerializeField] [Tooltip("Optional, depending on implementation")] protected Quest quest = null;
        [SerializeField] [Tooltip("Optional, depending on implementation")] protected QuestObjective objective = null;

        public abstract bool? Evaluate(QuestList questList);
    }
}
