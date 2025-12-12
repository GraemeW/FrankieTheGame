using UnityEngine;
using Frankie.Core;
using Frankie.Utils;

namespace Frankie.Quests
{
    public class QuestCompleter : MonoBehaviour, IQuestEvaluator
    {
        // Tunables
        [SerializeField][Tooltip("Optional for fixed quest")] protected QuestObjective questObjective;

        // Cached Reference
        private ReInitLazyValue<QuestList> questList;

        #region StaticMethods
        private static QuestList SetupQuestList() => Player.FindPlayerObject()?.GetComponent<QuestList>();
        #endregion
        
        #region UnityMethods
        private void Awake()
        {
            questList = new ReInitLazyValue<QuestList>(SetupQuestList);
        }

        private void Start()
        {
            questList.ForceInit();
        }
        #endregion

        #region PublicMethods
        public void CompleteObjective()
        {
            if (questObjective == null) { return; }
            questList.value.CompleteObjective(questObjective);
        }
        #endregion
    }
}
