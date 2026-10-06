namespace Frankie.Inventory.UI
{
    public readonly struct KnapsackSlot
    {
        public readonly int index;
        public readonly InventoryItem item;
        public readonly bool isEquipped;

        public KnapsackSlot(int index, InventoryItem item, bool isEquipped)
        {
            this.index = index;
            this.item = item;
            this.isEquipped = isEquipped;
        }

        public bool hasItem => item != null;
    }
}
