namespace Frankie.Inventory
{
    public class ActiveInventoryItem
    {
        InventoryItem inventoryItem = null;
        bool equipped = false;

        public ActiveInventoryItem(InventoryItem inventoryItem)
        {
            this.inventoryItem = inventoryItem;
            equipped = false;
        }

        public InventoryItem GetInventoryItem()
        {
            return inventoryItem;
        }

        public void SetEquipped(bool equipped)
        {
            this.equipped = equipped;
        }

        public bool IsEquipped()
        {
            return equipped;
        }
    }
}
