using UnityEditor;
using LowDefMustard.Localization;
using Frankie.Core.GameStateModifiers;

namespace Frankie.Inventory.Editor
{
    [CustomEditor(typeof(InventoryItem), true)]
    public class InventoryItemEditor : GameStateModifierEditor
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            
            LocalizationTool.InitializeEnglishLocale();
            var inventoryItem = (InventoryItem)target;
            if (inventoryItem is not ILocalizable localizable) { return; }
            localizable.TryLocalizeStandardEntries(inventoryItem, inventoryItem.GetPropertyLinkedLocalizationEntries());
        }
    }
}
