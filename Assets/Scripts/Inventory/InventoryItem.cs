using System.Collections.Generic;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Core;
using LowDefMustard.GameStateModifiers;
using LowDefMustard.Utils;
using LowDefMustard.Localization;
using Frankie.Speech;
using Frankie.Utils.Localization;

namespace Frankie.Inventory
{
    public abstract partial class InventoryItem : GameStateModifier, IAddressablesCache, ILocalizable, IDialogueVariable
    {
        // Config Data
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Inventory, true)] private LocalizedString localizedDisplayName;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Inventory, true)] private LocalizedString localizedDetail;
        [SerializeField][Tooltip("Overwritten for Key Items")] protected bool droppable = true;
        [SerializeField][Min(0)] private int price = 0;

        // State
        [HideInInspector] [SerializeField] private string cachedName;
        public string iCachedName { get => cachedName; set => cachedName = value; }
        [AutoStaticsCleanup] private static AddressablesLookupCache<InventoryItem> _cache = new(LDMLogCategory.Inventory, nameof(InventoryItem), inventoryItem => inventoryItem.guid);

        #region Getters
        public string GetDisplayName() => localizedDisplayName.GetLocalizedString();
        public string GetDetail() => localizedDetail.GetLocalizedString();
        public bool IsDroppable() => droppable;
        public int GetPrice() => price;
        #endregion
        
        #region DialogueVariable
        public string GetDialogueVariable(DialogueVariableContext context) => localizedDisplayName.GetSafeLocalizedString();
        #endregion
        
        #region Localization
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
        public static IAddressablesCacheLifecycle addressablesCache => _cache;
        public static InventoryItem GetFromID(string itemID) => _cache.Get(itemID);
        #endregion
    }
}
