using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory.UI
{
    public interface IUIItemHandler
    {
        public event Action<Enum> uiBoxStateChanged;
        void SetupItem(GameObject inventoryItemFieldPrefab, Transform container, int selector);
    }
}
