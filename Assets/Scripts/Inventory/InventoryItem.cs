using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Core.GameStateModifiers;
using Frankie.Utils.Addressables;
using Frankie.Utils.Localization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Frankie.Inventory
{
    public abstract class InventoryItem : GameStateModifier, IAddressablesCache, ILocalizable
    {
        // Config Data
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Inventory, true)] private LocalizedString localizedDisplayName;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Inventory, true)] private LocalizedString localizedDetail;
        [SerializeField][Tooltip("Overwritten for Key Items")] protected bool droppable = true;
        [SerializeField][Min(0)] private int price = 0;

        // State
        [HideInInspector] [SerializeField] private string cachedName;
        public string iCachedName { get => cachedName; set => cachedName = value; }
        private static AsyncOperationHandle<IList<InventoryItem>> _addressablesLoadHandle;
        private static Dictionary<string, InventoryItem> _itemLookupCache;

        #region Getters
        public string GetDisplayName() => localizedDisplayName.GetLocalizedString();
        public string GetDetail() => localizedDetail.GetLocalizedString();
        public bool IsDroppable() => droppable;
        public int GetPrice() => price;

        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.Inventory;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedDisplayName.TableEntryReference,
                localizedDetail.TableEntryReference
            };
        }
        
        public List<(string propertyName, LocalizedString localizedString, bool setToName)> GetPropertyLinkedLocalizationEntries()
        {
            return new List<(string propertyName, LocalizedString localizedString, bool setToName)>
            {
                (nameof(localizedDisplayName), localizedDisplayName, true),
                (nameof(localizedDetail), localizedDetail, false)
            };
        }
        #endregion
        
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
    }
}
