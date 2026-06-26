using System;

namespace Frankie.Inventory
{
    [Serializable]
    public class SaveableActiveItem
    {
        public string inventoryItemID;
        public bool equipped;
    }
}
