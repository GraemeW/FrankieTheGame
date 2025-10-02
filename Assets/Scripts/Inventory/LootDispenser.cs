using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils;

namespace Frankie.Inventory
{
    public class LootDispenser : MonoBehaviour
    {
        // Tunables
        [Header("Item Loot")]
        [SerializeField][Range(0, 10)] int minItems = 0;
        [SerializeField][Range(0, 10)] int maxItems = 1;
        [SerializeField] LootEntry<InventoryItem>[] lootEntries = null;
        [Header("Cash Loot")]
        [SerializeField][Min(0)] int minCash = 0;
        [SerializeField][Min(0)] int maxCash = 10;

        // Static
        static int ABSOLUTE_MAX_LOOT = 10;

        // Data Structures
        [System.Serializable]
        public class LootEntry<T> : IObjectProbabilityPair<T> where T : InventoryItem
        {
            [SerializeField] public InventoryItem inventoryItem = null;
            [SerializeField][Min(1)] public int probability = 1;

            public LootEntry(InventoryItem inventoryItem, int probability)
            {
                this.inventoryItem = inventoryItem;
                this.probability = probability;
            }

            public T GetObject()
            {
                return inventoryItem as T;
            }

            public int GetProbability()
            {
                return probability;
            }
        }

        // Methods
        public bool HasLootReward()
        {
            return (lootEntries != null && lootEntries.Length > 0) || (maxCash > 0);
        }

        public IEnumerable<InventoryItem> GetItemReward()
        {
            if (lootEntries == null || lootEntries.Length == 0) { yield break; }

            // Edge case protection
            int minimum = Mathf.Max(0, minItems);
            int maximum = Mathf.Max(minimum, maxItems);
            maximum = Mathf.Min(maximum, ABSOLUTE_MAX_LOOT); // Things get really tedious if we go too high

            int numberOfItems = Random.Range(minimum, maximum + 1); // +1 offset since random exclusive w/ ints

            for (int i = 0; i < numberOfItems; i++)
            {
                InventoryItem inventoryItem = GetInventoryItemFromLootTable();
                if (inventoryItem == null) { continue; }

                yield return inventoryItem;
            }
        }

        public InventoryItem GetInventoryItemFromLootTable()
        {
            InventoryItem inventoryItem = ProbabilityPairOperation<InventoryItem>.GetRandomObject(lootEntries);
            return inventoryItem;
        }

        public int GetCashReward()
        {
            // Edge case protection
            int minimum = Mathf.Max(0, minCash);
            int maximum = Mathf.Max(minimum, maxCash);
            if (maximum == 0) { return 0; }

            return Random.Range(minimum, maximum + 1); // +1 offset since random exclusive w/ ints
        }
    }
}
