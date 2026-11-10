using System.Collections.Generic;
using UnityEngine;
using LowDefMustard.GameStateModifiers;

namespace Frankie.Quests
{
    [CreateAssetMenu(fileName = "New Quest Completed Predicate", menuName = "Predicates/QuestList/Quest Completed", order = 5)]
    public class QuestCompletedPredicate : PredicateQuestList, IGameStateModifierPredicate
    {
        [SerializeField] private Quest quest;

        // Editor Field References
        internal const string questField = nameof(quest);
        
        public override bool? Evaluate(QuestList questList)
        {
            if (questList == null) { return null; }
            if (quest == null) { return false; }
            
            QuestStatus questStatus = questList.GetQuestStatus(quest);
            return questStatus != null && questStatus.IsComplete();
        }

        public IEnumerable<GameStateModifier> GetGameStateModifiers()
        {
            if (quest != null) { yield return quest; }
        }
    }
}
