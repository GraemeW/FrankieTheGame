using UnityEngine;

namespace Frankie.Inventory
{
    [CreateAssetMenu(menuName = ("Inventory/Wearable Item"))]
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
