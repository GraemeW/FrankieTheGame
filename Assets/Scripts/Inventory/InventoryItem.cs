using Frankie.Core;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Frankie.Inventory
{
    public abstract class InventoryItem : ScriptableObject, ISerializationCallbackReceiver, IAddressablesCache
    {
        // Config Data
        [Tooltip("Auto-generated UUID for saving/loading -- clear to generate new, write to generate fixed")]
        [SerializeField] string itemID = null;
        [Tooltip("Item name displayed in UI")]
        [SerializeField] string displayName = null;
        [Tooltip("Item description on inspection")]
        [SerializeField] [TextArea] string description = null;
        [SerializeField] [Tooltip("Overwritten for Key Items")] protected bool droppable = true;
        [SerializeField] [Min(0)] int price = 0;

        // State
        static AsyncOperationHandle<IList<InventoryItem>> addressablesLoadHandle;
        static Dictionary<string, InventoryItem> itemLookupCache;

        #region AddressablesCaching
        public static InventoryItem GetFromID(string itemID)
        {
            if (string.IsNullOrWhiteSpace(itemID)) { return null; }

            BuildCacheIfEmpty();
            if (itemID == null || !itemLookupCache.ContainsKey(itemID)) return null;
            return itemLookupCache[itemID];
        }

        public static void BuildCacheIfEmpty()
        {
            if (itemLookupCache == null)
            {
                BuildInventoryItemCache();
            }
        }

        private static void BuildInventoryItemCache()
        {
            itemLookupCache = new Dictionary<string, InventoryItem>();
            addressablesLoadHandle = Addressables.LoadAssetsAsync(typeof(InventoryItem).Name, (InventoryItem item) =>
            {
                if (itemLookupCache.ContainsKey(item.itemID))
                {
                    Debug.LogError(string.Format("Looks like there's a duplicate ID for objects: {0} and {1}", itemLookupCache[item.itemID], item));
                }

                itemLookupCache[item.itemID] = item;
            }
            );
            addressablesLoadHandle.WaitForCompletion();
        }

        public static void ReleaseCache()
        {
            Addressables.Release(addressablesLoadHandle);
        }
        #endregion

        #region PublicMethods
        public static string GetItemNamePretty(string itemName)
        {
            return Regex.Replace(itemName, "([a-z])_?([A-Z])", "$1 $2");
        }

        public string GetItemID()
        {
            return itemID;
        }

        public string GetDisplayName()
        {
            return displayName;
        }

        public string GetDescription()
        {
            return description;
        }

        public bool IsDroppable()
        {
            return droppable;
        }

        public int GetPrice()
        {
            return price;
        }
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