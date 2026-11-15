using UnityEngine;

namespace Frankie.Inventory
{
    [CreateAssetMenu(fileName = "EquipableItem", menuName = "Inventory/Equipable Item", order = 6)]
    public class EquipableItem : EquipableItemBase
    {
        [Header("Equipable Item Properties")]
        [SerializeField] private EquipLocation equipLocation;
        public override EquipLocation GetEquipLocation() => equipLocation;
    }
}
