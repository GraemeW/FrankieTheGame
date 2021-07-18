using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory
{
    [CreateAssetMenu(menuName = ("Inventory/Key Item"))]
    public class KeyItem : InventoryItem
    {
        private void Awake()
        {
            droppable = false;
        }
    }
}
