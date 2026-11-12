using System.Collections.Generic;
using UnityEngine;
using LowDefMustard.GameStateModifiers;

namespace Frankie.Quests
{
    [CreateAssetMenu(fileName = "New Has Quest Predicate", menuName = "Predicates/QuestList/Has Quest", order = 5)]
    public class HasQuestPredicate : PredicateQuestList, IGameStateModifierPredicate
    {
        [SerializeField] private Quest quest;

        // Editor Field References
        internal const string questField = nameof(quest);
        
        public override bool? Evaluate(QuestList questList)
        {
            if (questList == null) { return null; }
            return quest != null && questList.HasQuest(quest);
        }

        public IEnumerable<GameStateModifier> GetGameStateModifiers()
        {
            if (quest != null) { yield return quest; }
        }
    }
}
