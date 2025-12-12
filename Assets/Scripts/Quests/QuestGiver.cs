using UnityEngine;
using Frankie.Utils;
using Frankie.Core;

namespace Frankie.Quests
{
    public class QuestGiver : MonoBehaviour
    {
        // Tunables
        [SerializeField][Tooltip("Optional for fixed quest")] private Quest quest;

        // Cached References
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
        public void GiveQuest()
        {
            GiveQuest(quest);
        }

        public void GiveQuest(Quest questToGive)
        {
            if (questToGive == null) { return; }
            questList.value.AddQuest(questToGive);
        }
        #endregion
    }
}
