using Frankie.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Quests
{
    public class QuestCompleter : MonoBehaviour, IQuestEvaluator
    {
        // Tunables
        [SerializeField] [Tooltip("Optional for fixed quest")] protected Quest quest = null;
        [SerializeField] [Tooltip("Optional for fixed quest")] protected QuestObjective objective = null;

        // State
        bool questListPreInitialized = false;

        // Cached References
        GameObject player = null;
        protected ReInitLazyValue<QuestList> questList = null;

        private void Awake()
        {
            PreInitializeQuestList();
        }

        private void PreInitializeQuestList()
        {
            // Special handling
            // Start vs. Awake order of operations break on game object enable/disable
            if (questListPreInitialized) { return; }

            player = GameObject.FindGameObjectWithTag("Player");
            questList = new ReInitLazyValue<QuestList>(() => QuestList.GetQuestList(ref player));
            questListPreInitialized = true;
        }

        private void Start()
        {
            PreInitializeQuestList();
            questList.ForceInit();
        }

        public void CompleteObjective()
        {
            if (quest == null || objective == null) { return; }

            CompleteObjective(quest, objective);
        }

        public void CompleteObjective(Quest quest, QuestObjective objective)
        {
            questList.value.CompleteObjective(quest, objective);
        }
    }
}