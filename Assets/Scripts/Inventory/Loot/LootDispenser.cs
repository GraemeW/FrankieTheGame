using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using LowDefMustard.GameStateModifiers;
using LowDefMustard.Utils;

namespace Frankie.Inventory
{
    [ExecuteInEditMode]
    public class LootDispenser : MonoBehaviour, IGameStateModifierHandler
    {
        // Tunables
        [Header("Item Loot")]
        [SerializeField][Range(0, 10)] private int minItems = 0;
        [SerializeField][Range(0, 10)] private int maxItems = 1;
        [SerializeField] private List<LootEntry> lootEntries = new();
        [Header("Cash Loot")]
        [SerializeField][Min(0)] private int minCash = 0;
        [SerializeField][Min(0)] private int maxCash = 10;

        // GameState Modifier Properties
        [SerializeField][HideInInspector] private string backingHandlerGUID;
        public string handlerGUID { get => backingHandlerGUID; set => backingHandlerGUID = value; }
        [SerializeField][HideInInspector] private int backingListHashCheck;
        public int modifierListHashCheck { get => backingListHashCheck; set => backingListHashCheck = value; }
        [SerializeField][HideInInspector] private bool backingHasGameStateModifiers;
        public bool hasGameStateModifiers { get => backingHasGameStateModifiers; set => backingHasGameStateModifiers = value; }
        [SerializeField] [HideInInspector] private List<string> backingGameStateModifierGUIDs;
        public List<string> gameStateModifierGUIDs { get => backingGameStateModifierGUIDs; set => backingGameStateModifierGUIDs = value ?? new List<string>(); } 
        
        // Static
        private const int _absoluteMaxLoot = 10;

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
                if (!lootEntries.TryGetRandom(out InventoryItem inventoryItem)) { continue; }

                yield return inventoryItem;
            }
        }

        public int GetCashReward()
        {
            // Edge case protection
            int minimum = Mathf.Max(0, minCash);
            int maximum = Mathf.Max(minimum, maxCash);
            return maximum == 0 ? 0 : Random.Range(minimum, maximum + 1); // +1 offset since random exclusive w/ ints
        }
        #endregion
        
        #region InterfaceMethods
        public IList<GameStateModifier> GetGameStateModifiers()
        {
            List<GameStateModifier> gameStateModifiers = new();
            foreach (LootEntry lootEntry in lootEntries)
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
