using System;
using UnityEngine;

namespace Frankie.Inventory.UI
{
    public interface IUIItemHandler
    {
        public event Action<Enum> uiBoxStateChanged;
        InventoryItemField SetupItem(InventoryItemField inventoryItemFieldPrefab, Transform container, int selector);
    }
}
