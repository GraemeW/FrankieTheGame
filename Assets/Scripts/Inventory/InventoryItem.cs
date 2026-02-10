using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Frankie.Core;

namespace Frankie.Inventory
{
    public abstract class InventoryItem : ScriptableObject, ISerializationCallbackReceiver, IAddressablesCache
    {
        // Config Data
        [Tooltip("Auto-generated UUID for saving/loading -- clear to generate new, write to generate fixed")]
        [SerializeField] private string itemID;
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
                if (_itemLookupCache.TryGetValue(inventoryItem.itemID, out InventoryItem matchInventoryItem))
                {
                    Debug.LogError($"Looks like there's a duplicate ID for objects: {matchInventoryItem} and {inventoryItem}");
                }

                _itemLookupCache[inventoryItem.itemID] = inventoryItem;
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
        public static string GetItemNamePretty(string itemName) => Regex.Replace(itemName, "([a-z])_?([A-Z])", "$1 $2");
        public string GetItemID() => itemID;
        public string GetDisplayName() => displayName;
        public string GetDescription() => description;
        public bool IsDroppable() => droppable;
        public int GetPrice() => price;
        #endregion

        #region UnityMethods
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // Generate and save a new UUID if this is blank
            if (string.IsNullOrWhiteSpace(itemID))
            {
                itemID = System.Guid.NewGuid().ToString();
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Unused, required for interface
        }
        #endregion
    }
}
