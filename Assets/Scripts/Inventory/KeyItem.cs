using Frankie.Quests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory
{
    [CreateAssetMenu(menuName = ("Inventory/Key Item"))]
    public class KeyItem : InventoryItem
    {
        [SerializeField] QuestObjectivePair[] questObjectivePairs = null;
        
        private void Awake()
        {
            droppable = false;
        }

        public QuestObjectivePair[] GetQuestObjectivePairs()
        {
            return questObjectivePairs;
        }
    }
}
