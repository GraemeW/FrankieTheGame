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

        public InventoryItem GetInventoryItem() => inventoryItem;
        public bool IsEquipped() => equipped;

        public void SetEquipped(bool setEquipped)
        {
            equipped = setEquipped;
        }
    }
}
