using UnityEngine;
using Frankie.Utils;
using Frankie.Core;

namespace Frankie.Quests
{
    public class QuestHandler : MonoBehaviour
    {
        // Tunables
        [SerializeField][Tooltip("Configure for Give Quest")] private Quest quest;
        [SerializeField][Tooltip("Configure for Complete Objective")] protected QuestObjective questObjective;

        // Cached References
        private ReInitLazyValue<QuestList> questList;

        #region StaticMethods

        private static QuestList SetupQuestList()
        {
            GameObject playerObject = Player.FindPlayerObject();
            return playerObject == null ? null : playerObject.GetComponent<QuestList>();
        }
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
        public void GiveConfiguredQuest()
        {
            if (quest == null) { return; }
            questList.value.TryAddQuest(quest);
        }
        
        public void CompleteConfiguredObjective()
        {
            if (questObjective == null) { return; }
            questList.value.CompleteObjective(questObjective);
        }
        #endregion
    }
}
