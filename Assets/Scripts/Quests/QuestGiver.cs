using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils;
using Frankie.Control;

namespace Frankie.Quests
{
    public class QuestGiver : MonoBehaviour
    {
        // Tunables
        [SerializeField][Tooltip("Optional for fixed quest")] Quest quest = null;

        // Cached References
        GameObject player = null;
        ReInitLazyValue<QuestList> questList = null;

        private void Awake()
        {
            player = GameObject.FindGameObjectWithTag("Player");
            questList = new ReInitLazyValue<QuestList>(SetupQuestList);
        }

        private void Start()
        {
            questList.ForceInit();
        }

        private QuestList SetupQuestList()
        {
            if (player == null) { player = GameObject.FindGameObjectWithTag("Player"); }
            return player.GetComponent<QuestList>();
        }

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