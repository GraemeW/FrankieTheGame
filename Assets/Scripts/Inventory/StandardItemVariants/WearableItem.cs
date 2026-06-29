using UnityEngine;

namespace Frankie.Inventory
{
    [CreateAssetMenu(fileName = "WearableItem", menuName = "Inventory/Wearable Item", order = 10)]
    public class WearableItem : EquipableItemBase
    {
        // Tunables
        [Header("Wearable Item Properties - Note:  EquipLocation = Other")]
        [SerializeField] private Wearable wearablePrefab;

        // Getters
        public Wearable GetWearablePrefab() => wearablePrefab;
        public override EquipLocation GetEquipLocation() => EquipLocation.Other;
    }
}
