using LowDefMustard.Utils;
using UnityEngine;

namespace Frankie.Inventory
{
    [System.Serializable]
    public class LootEntry : IObjectProbabilityPair<InventoryItem>
    {
        [SerializeField] public InventoryItem inventoryItem;
        [SerializeField][Min(1)] public int probabilityWeight = 1;

        public LootEntry(InventoryItem inventoryItem, int probabilityWeight)
        {
            this.inventoryItem = inventoryItem;
            this.probabilityWeight = probabilityWeight;
        }
        public InventoryItem value => inventoryItem;
        public int weight => probabilityWeight;
    }
}