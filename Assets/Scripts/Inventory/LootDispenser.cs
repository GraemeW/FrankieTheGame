using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using Frankie.Utils;
using Frankie.Core.GameStateModifiers;

namespace Frankie.Inventory
{
    [ExecuteInEditMode]
    public class LootDispenser : MonoBehaviour, IGameStateModifierHandler
    {
        // Interface Properties
        [SerializeField] private string backingHandlerGUID;
        public string handlerGUID { get => backingHandlerGUID; set => backingHandlerGUID = value; }
        
        [SerializeField][HideInInspector] private int backingListHashCheck;
        public int modifierListHashCheck { get => backingListHashCheck; set => backingListHashCheck = value; }
        
        [SerializeField][HideInInspector] private bool backingHasGameStateModifiers;
        public bool hasGameStateModifiers { get => backingHasGameStateModifiers; set => backingHasGameStateModifiers = value; }
        
        [SerializeField] [HideInInspector] private List<string> backingGameStateModifierGUIDs;
        public List<string> gameStateModifierGUIDs { get => backingGameStateModifierGUIDs; set => backingGameStateModifierGUIDs = value ?? new List<string>(); } 
        
        // Tunables
        [Header("Item Loot")]
        [SerializeField][Range(0, 10)] private int minItems = 0;
        [SerializeField][Range(0, 10)] private int maxItems = 1;
        [SerializeField] private List<LootEntry<InventoryItem>> lootEntries = new();
        [Header("Cash Loot")]
        [SerializeField][Min(0)] private int minCash = 0;
        [SerializeField][Min(0)] private int maxCash = 10;

        // Static
        private const int _absoluteMaxLoot = 10;

        #region DataStructures
        [System.Serializable]
        public class LootEntry<T> : IObjectProbabilityPair<T> where T : InventoryItem
        {
            [SerializeField] public InventoryItem inventoryItem;
            [SerializeField][Min(1)] public int probability = 1;

            public LootEntry(InventoryItem inventoryItem, int probability)
            {
                this.inventoryItem = inventoryItem;
                this.probability = probability;
            }
            public T GetObject() => inventoryItem as T;
            public int GetProbability() => probability;
        }
        #endregion

        #region UnityMethods

        private void OnDestroy()
        {
            IGameStateModifierHandler.TriggerOnDestroy(this);
        }
        
        private void OnDrawGizmos()
        {
            IGameStateModifierHandler.TriggerOnGizmos(this);
        }
        #endregion
        
        #region PublicMethods
        public bool HasLootReward() => lootEntries.Count > 0 || maxCash > 0;
        public InventoryItem GetInventoryItemFromLootTable() => lootEntries.Count > 0 ? ProbabilityPairOperation<InventoryItem>.GetRandomObject(lootEntries) : null;

        public IEnumerable<InventoryItem> GetItemReward()
        {
            if (lootEntries.Count == 0) { yield break; }

            // Edge case protection
            int minimum = Mathf.Max(0, minItems);
            int maximum = Mathf.Max(minimum, maxItems);
            maximum = Mathf.Min(maximum, _absoluteMaxLoot); // Things get really tedious if we go too high

            int numberOfItems = Random.Range(minimum, maximum + 1); // +1 offset since random exclusive w/ ints

            for (int i = 0; i < numberOfItems; i++)
            {
                InventoryItem inventoryItem = GetInventoryItemFromLootTable();
                if (inventoryItem == null) { continue; }

                yield return inventoryItem;
            }
        }

        public int GetCashReward()
        {
            // Edge case protection
            int minimum = Mathf.Max(0, minCash);
            int maximum = Mathf.Max(minimum, maxCash);
            if (maximum == 0) { return 0; }

            return Random.Range(minimum, maximum + 1); // +1 offset since random exclusive w/ ints
        }
        #endregion

        #region InterfaceMethods
        public IList<GameStateModifier> GetGameStateModifiers()
        {
            List<GameStateModifier> gameStateModifiers = new();
            foreach (LootEntry<InventoryItem> lootEntry in lootEntries)
            {
                if (lootEntry.inventoryItem == null) { continue; }
                
                var keyItem = lootEntry.inventoryItem as KeyItem;
                if (keyItem == null) { continue; }
                
                gameStateModifiers.Add(keyItem);
            }
            return gameStateModifiers;
        }
        #endregion
    }
}
