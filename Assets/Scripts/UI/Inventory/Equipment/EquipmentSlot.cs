namespace Frankie.Inventory.UI
{
    public readonly struct EquipmentSlot
    {
        public readonly EquipLocation equipLocation;
        public readonly EquipableItemBase item;

        public EquipmentSlot(EquipLocation equipLocation, EquipableItemBase item)
        {
            this.equipLocation = equipLocation;
            this.item = item;
        }

        public bool hasItem => item != null;
    }
}
