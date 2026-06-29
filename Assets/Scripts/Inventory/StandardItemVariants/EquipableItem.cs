using UnityEngine;

namespace Frankie.Inventory
{
    [CreateAssetMenu(fileName = "EquipableItem", menuName = "Inventory/Equipable Item", order = 10)]
    public class EquipableItem : EquipableItemBase
    {
        [SerializeField] protected EquipLocation equipLocation;
        public override EquipLocation GetEquipLocation() => equipLocation;
    }
}
