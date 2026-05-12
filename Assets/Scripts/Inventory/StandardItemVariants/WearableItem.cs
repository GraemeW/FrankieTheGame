using UnityEngine;

namespace Frankie.Inventory
{
    [CreateAssetMenu(fileName = "WearableItem", menuName = "Inventory/Wearable Item", order = 10)]
    public class WearableItem : InventoryItem
    {
        // Tunables
        [SerializeField] private Wearable wearablePrefab;
        [SerializeField] private bool isUnique = true;

        // Getters
        public Wearable GetWearablePrefab() => wearablePrefab;
        public bool IsUnique() => isUnique;
    }
}
