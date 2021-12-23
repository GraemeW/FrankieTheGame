using Frankie.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frankie.Inventory
{
    public class LootDispenser : MonoBehaviour
    {
        // Tunables
        [SerializeField] [Range(0, 10)] int minItems = 0;
        [SerializeField] [Range(0, 10)] int maxItems = 1;
        [SerializeField] LootEntry<InventoryItem>[] lootEntries = null;

        // Static
        int ABSOLUTE_MAX_LOOT = 10;

        // Data Structures
        [System.Serializable]
        public class LootEntry<T> : IObjectProbabilityPair<T> where T : InventoryItem
        {
            public InventoryItem inventoryItem;
            public int probability;

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
        public IEnumerable<InventoryItem> GetLoot()
        {
            if (lootEntries == null || lootEntries.Length == 0) { yield break; }

            // Edge case protection
            int minimum = Mathf.Max(0, minItems);
            int maximum = Mathf.Max(minimum, maxItems);
            maximum = Mathf.Min(maximum, ABSOLUTE_MAX_LOOT); // Things get really tedious if we go too high

            int numberOfItems = Random.Range(minimum, maximum + 1); // +1 offset since random exclusive w/ ints

            for (int i = 0; i < numberOfItems; i++)
            {
                yield return GetInventoryItemFromLootTable();
            }
        }

        public InventoryItem GetInventoryItemFromLootTable()
        {
            InventoryItem inventoryItem = ProbabilityPairOperation<InventoryItem>.GetRandomObject(lootEntries);
            return inventoryItem;
        }
    }
}