using Frankie.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Quests
{
    public class QuestCompleter : MonoBehaviour
    {
        // Tunables
        [SerializeField] [Tooltip("Optional for fixed quest")] Quest quest = null;
        [SerializeField] [Tooltip("Optional for fixed quest")] string objectiveID = null;

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

        public void CompleteObjective()
        {
            if (quest == null || string.IsNullOrWhiteSpace(objectiveID)) { return; }

            CompleteObjective(quest, objectiveID);
        }

        public void CompleteObjective(Quest quest, string objectiveID)
        {
            questList.value.CompleteObjective(quest, objectiveID);
        }
    }
}