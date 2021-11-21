using Frankie.Quests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Core
{
    public abstract class PredicateQuestList : Predicate
    {
        [SerializeField] [Tooltip("Optional, depending on implementation")] protected Quest quest = null;
        [SerializeField] [Tooltip("Optional, depending on implementation")] protected string objective = null;

        public abstract bool? Evaluate(QuestList questList);
    }
}
