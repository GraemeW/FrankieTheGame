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
        #endregion
        
#if UNITY_EDITOR
        #region LocalizationUtility
        private string GetNameLocalizationKey() => GetNameLocalizationKey(this, name);
        public static string GetNameLocalizationKey(Object targetObject, string id) => $"{targetObject.GetType().Name}.{id}";
        private string GetDetailLocalizationKey() => $"{GetNameLocalizationKey()}.Detail";
        public static string GetDetailLocalizationKey(Object targetObject, string id) => $"{GetNameLocalizationKey(targetObject, id)}.Detail";
        
        private void ReconcileCachedName()
        {
            if (name == cachedName) { return; }

            TableEntryReference oldNameKey = GetNameLocalizationKey(this, cachedName);
            TableEntryReference oldDetailKey = GetDetailLocalizationKey(this, cachedName);
            cachedName = name;
            string newNameKey = GetNameLocalizationKey();
            string newDetailKey = GetDetailLocalizationKey();
            LocalizationTool.MakeOrRenameKey(localizationTableType, oldNameKey, newNameKey);
            LocalizationTool.MakeOrRenameKey(localizationTableType, oldDetailKey, newDetailKey);
            
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        public void TryLocalizeDefaults()
        {
            ReconcileCachedName();
            string nameKey = GetNameLocalizationKey();
            string detailKey = GetDetailLocalizationKey();
            bool wasNameLocalizationUpdated = LocalizationTool.TryLocalizeEntry(localizationTableType, localizedDisplayName, nameKey, name);
            bool wasDetailKeyLinked = LocalizationTool.InitializeLocalEntry(localizationTableType, localizedDetail, detailKey);
            if (!wasNameLocalizationUpdated && !wasDetailKeyLinked) { return; }
            
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }
        #endregion
#endif
        
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
