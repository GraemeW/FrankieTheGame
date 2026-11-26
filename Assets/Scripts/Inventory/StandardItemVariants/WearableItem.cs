using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Frankie.Inventory
{
    [CreateAssetMenu(fileName = "WearableItem", menuName = "Inventory/Wearable Item", order = 8)]
    public class WearableItem : EquipableItemBase
    {
        // Tunables
        [Header("Wearable Item Properties - Note:  EquipLocation = Other")]
        [SerializeField] private AssetReferenceGameObject wearablePrefab; // Wearable prefab, loaded on demand (via WearablesLink)
        [SerializeField] private bool shouldColliderMatchPlayer = true;

        // Field References
        internal const string wearablePrefabField = nameof(wearablePrefab);

        // Getters
        public bool ShouldColliderMatchPlayer() => shouldColliderMatchPlayer;
        public override EquipLocation GetEquipLocation() => EquipLocation.Other;
        public AssetReferenceGameObject GetWearablePrefab() => wearablePrefab;
    }
}
