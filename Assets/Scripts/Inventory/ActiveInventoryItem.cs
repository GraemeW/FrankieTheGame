namespace Frankie.Inventory
{
    public class ActiveInventoryItem
    {
        private readonly InventoryItem inventoryItem;
        private bool equipped = false;

        public ActiveInventoryItem(InventoryItem inventoryItem)
        {
            this.inventoryItem = inventoryItem;
            equipped = false;
        }

        public InventoryItem GetInventoryItem()
        {
            return inventoryItem;
        }

        public void SetEquipped(bool setEquipped)
        {
            equipped = setEquipped;
        }

        public bool IsEquipped() => equipped;
    }
}
