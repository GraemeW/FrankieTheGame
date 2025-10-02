using UnityEngine;
using Frankie.Quests;

namespace Frankie.Inventory
{
    [CreateAssetMenu(menuName = ("Inventory/Key Item"))]
    public class KeyItem : InventoryItem
    {
        [SerializeField] QuestObjective[] questObjectives = new QuestObjective[0];

        private void Awake()
        {
            droppable = false;
        }

        public QuestObjective[] GetQuestObjectives() => questObjectives;
    }
}
