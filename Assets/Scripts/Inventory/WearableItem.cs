using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory
{
    [CreateAssetMenu(menuName = ("Inventory/Wearable Item"))]
    public class WearableItem : InventoryItem
    {
        // Tunables
        [SerializeField] Wearable wearablePrefab = null;

        // Getters
        public Wearable GetWearablePrefab() => wearablePrefab;
    }
}
