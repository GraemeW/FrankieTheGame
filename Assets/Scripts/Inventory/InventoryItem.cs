using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Frankie.Inventory
{
    public abstract class InventoryItem : ScriptableObject, ISerializationCallbackReceiver
    {
        // Config Data
        [Tooltip("Auto-generated UUID for saving/loading -- clear to generate new, write to generate fixed")]
        [SerializeField] string itemID = null;
        [Tooltip("Item name displayed in UI")]
        [SerializeField] string displayName = null;
        [Tooltip("Item description on inspection")]
        [SerializeField] [TextArea] string description = null;
        [SerializeField] protected bool droppable = true;

        // State
        static Dictionary<string, InventoryItem> itemLookupCache;

        public static InventoryItem GetFromID(string itemID)
        {
            if (itemLookupCache == null)
            {
                itemLookupCache = new Dictionary<string, InventoryItem>();
                var itemList = Resources.LoadAll<InventoryItem>("");
                foreach (var item in itemList)
                {
                    if (itemLookupCache.ContainsKey(item.itemID))
                    {
                        Debug.LogError(string.Format("Looks like there's a duplicate ID for objects: {0} and {1}", itemLookupCache[item.itemID], item));
                        continue;
                    }

                    itemLookupCache[item.itemID] = item;
                }
            }

            if (itemID == null || !itemLookupCache.ContainsKey(itemID)) return null;
            return itemLookupCache[itemID];
        }

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
    }
}