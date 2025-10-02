using UnityEngine;
using Frankie.Core;
using Frankie.Utils;

namespace Frankie.Quests
{
    public class QuestCompleter : MonoBehaviour, IQuestEvaluator
    {
        // Tunables
        [SerializeField][Tooltip("Optional for fixed quest")] protected QuestObjective questObjective = null;

        protected ReInitLazyValue<QuestList> questList;

        private void Awake()
        {
            questList = new ReInitLazyValue<QuestList>(SetupQuestList);
        }

        private void Start()
        {
            questList.ForceInit();
        }

        private QuestList SetupQuestList() => Player.FindPlayerObject()?.GetComponent<QuestList>();

        public void CompleteObjective()
        {
            if (questObjective == null) { return; }

            questList.value.CompleteObjective(questObjective);
        }
    }
}
