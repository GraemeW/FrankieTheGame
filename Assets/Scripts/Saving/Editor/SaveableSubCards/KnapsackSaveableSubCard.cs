using UnityEngine.UIElements;
using Frankie.Inventory;

namespace Frankie.Saving.Editor
{
    public class KnapsackSaveableSubCard : SaveableSubCardData
    {
        public KnapsackSaveableSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }

        public override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not Knapsack knapsack) { return; }
            
            ActiveInventoryItem[] itemsInKnapsack = knapsack.ManualGetDataFromState(saveState);
            if (itemsInKnapsack == null || itemsInKnapsack.Length == 0)
            {
                // TODO:  Add label to note issue in loading knapsack save
                return;
            }
            
            for (int i = 0; i < itemsInKnapsack.Length; i++)
            {
                ActiveInventoryItem activeInventoryItem = itemsInKnapsack[i];
                // TODO:  Add editable fields, w/ value set by:
                if (activeInventoryItem != null)
                {
                    InventoryItem inventoryItem = activeInventoryItem.GetInventoryItem();
                    bool isEquipped = activeInventoryItem.IsEquipped();
                    // set value for editable field
                }
                
                // TODO:  Update editable field callbacks to update saveState via
                // itemsInKnapsack[i] = new ActiveInventoryItem(newInventoryItem);
                // itemsInKnapsack[i].SetEquipped(newEquipState);
                // saveState = knapsack.ManualGetStateFromData(itemsInKnapsack);
            }
        }
    }
}
