using UnityEngine;
using UnityEngine.UIElements;
using Frankie.Inventory;

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
                subCardView.Add(new Label("No Wallet save data found"));
                return;
            }

            int cash = saveData.cash;
            int pendingCash = saveData.pendingCash;

            var cashRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(cashRow);
            cashRow.Add(new Label("Cash:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            var cashField = new IntegerField { value = cash, style = { flexGrow = 1 } };
            cashRow.Add(cashField);

            var pendingCashRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(pendingCashRow);
            pendingCashRow.Add(new Label("Pending Cash:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            var pendingCashField = new IntegerField { value = pendingCash, style = { flexGrow = 1 } };
            pendingCashRow.Add(pendingCashField);

            cashField.RegisterValueChangedCallback(changeEvent =>
            {
                cash = changeEvent.newValue;
                var updatedSaveData = new WalletSaveData(cash, pendingCash);
                saveState = wallet.ManualGetStateFromData(updatedSaveData);
                RaiseSaveStateChanged();
            });

            pendingCashField.RegisterValueChangedCallback(changeEvent =>
            {
                pendingCash = changeEvent.newValue;
                var updatedSaveData = new WalletSaveData(cash, pendingCash);
                saveState = wallet.ManualGetStateFromData(updatedSaveData);
                RaiseSaveStateChanged();
            });
        }
    }
}
