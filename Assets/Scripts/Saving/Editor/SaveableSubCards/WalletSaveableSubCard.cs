using Frankie.Inventory;
using UnityEngine.UIElements;

namespace Frankie.Saving.Editor
{
    public class WalletSaveableSubCard : SaveableSubCardData
    {
        public WalletSaveableSubCard(ISaveableBase saveable, SaveState saveState)
        {
            this.saveable = saveable;
            this.saveState = saveState;
        }

        public override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not Wallet wallet) { return; }
            
            WalletSaveData saveData = wallet.ManualGetDataFromState(saveState);
            if (saveData == null)
            {
                // TODO:  Add label to note issue in loading wallet save
                return;
            }

            int cash = saveData.cash;
            int pendingCash = saveData.pendingCash;
            
            // TODO:  Add editable fields for cash, pendingCash
            
            // Update editable field callbacks to update saveState via:
            // var updatedSaveData = new WalletSaveData(newCash, newPendingCash);
            // saveState = wallet.ManualGetStateFromData(updatedSaveData);
        }
    }
}
