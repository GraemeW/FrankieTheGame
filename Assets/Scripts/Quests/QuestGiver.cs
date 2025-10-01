using UnityEngine;
using Frankie.Utils;
using Frankie.Core;

namespace Frankie.Quests
{
    public class QuestGiver : MonoBehaviour
    {
        // Tunables
        [SerializeField][Tooltip("Optional for fixed quest")] Quest quest = null;

        // Cached References
        ReInitLazyValue<QuestList> questList = null;

        private void Awake()
        {
            questList = new ReInitLazyValue<QuestList>(SetupQuestList);
        }

        private void Start()
        {
            questList.ForceInit();
        }

        private QuestList SetupQuestList() => Player.FindPlayerObject()?.GetComponent<QuestList>();

        public void GiveQuest()
        {
            GiveQuest(quest);
        }

        public void GiveQuest(Quest quest)
        {
            if (quest == null) { return; }

            questList.value.AddQuest(quest);
        }
    }
}
