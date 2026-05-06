using System.Collections.Generic;
using UnityEngine;
using Frankie.Quests;

namespace Frankie.Inventory
{
    [CreateAssetMenu(fileName = "KeyItem", menuName = "Inventory/Key Item", order = 10)]
    public class KeyItem : InventoryItem
    {
        [SerializeField] private List<QuestObjective> questObjectives = new();

        private void Awake()
        {
            droppable = false;
        }

        public IList<QuestObjective> GetQuestObjectives() => questObjectives;
    }
}
