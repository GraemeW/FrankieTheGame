using System.Collections.Generic;
using UnityEngine.UIElements;
using Frankie.Inventory;

namespace Frankie.Saving.Editor
{
    public class WearablesLinkSubCard : SaveableSubCardData
    {
        public WearablesLinkSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }
        
        public override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not WearablesLink wearablesLink) { return; }
            
            List<WearableItem> wearableItems = wearablesLink.ManualGetDataFromState(saveState);
            if (wearableItems == null || wearableItems.Count == 0)
            {
                // TODO:  Add label to note issue in loading wearablesLink save
                return;
            }

            for (int i = 0; i < wearableItems.Count; i++)
            {
                WearableItem wornItem = wearableItems[i];
                // TODO:  Add editable fields for wornItems
                
                // Update editable field callbacks to update via:
                // wornItem[i] = newWearableItem;
                // saveState = wearablesLink.ManualGetStateFromData(wornItems);
            }
        }
    }
}
