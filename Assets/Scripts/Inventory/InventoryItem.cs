using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Frankie.Core;
using Frankie.Core.GameStateModifiers;

namespace Frankie.Inventory
{
    public abstract class InventoryItem : GameStateModifier, IAddressablesCache
    {
        // Config Data
        [Tooltip("Item name displayed in UI")]
        [SerializeField] private string displayName;
        [Tooltip("Item description on inspection")]
        [SerializeField][TextArea] private string description;
        [SerializeField][Tooltip("Overwritten for Key Items")] protected bool droppable = true;
        [SerializeField][Min(0)] private int price = 0;

        // State
        private static AsyncOperationHandle<IList<InventoryItem>> _addressablesLoadHandle;
        private static Dictionary<string, InventoryItem> _itemLookupCache;

        #region AddressablesCaching
        public static InventoryItem GetFromID(string itemID)
        {
            if (string.IsNullOrWhiteSpace(itemID)) { return null; }

            BuildCacheIfEmpty();
            return _itemLookupCache.GetValueOrDefault(itemID);
        }

        public static void BuildCacheIfEmpty()
        {
            if (_itemLookupCache == null)
            {
                BuildInventoryItemCache();
            }
        }

        private static void BuildInventoryItemCache()
        {
            _itemLookupCache = new Dictionary<string, InventoryItem>();
            _addressablesLoadHandle = Addressables.LoadAssetsAsync(nameof(InventoryItem), (InventoryItem inventoryItem) =>
            {
                if (_itemLookupCache.TryGetValue(inventoryItem.guid, out InventoryItem matchInventoryItem))
                {
                    Debug.LogError($"Looks like there's a duplicate ID for objects: {matchInventoryItem} and {inventoryItem}");
                }

                _itemLookupCache[inventoryItem.guid] = inventoryItem;
            }
            );
            _addressablesLoadHandle.WaitForCompletion();
        }

        public static void ReleaseCache()
        {
            Addressables.Release(_addressablesLoadHandle);
        }
        #endregion

        #region PublicMethods
        public string GetDisplayName() => displayName;
        public string GetDescription() => description;
        public bool IsDroppable() => droppable;
        public int GetPrice() => price;
        #endregion
    }
}
