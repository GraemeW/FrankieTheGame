using UnityEngine;
using UnityEngine.UIElements;
using LowDefMustard.Saving;
using LowDefMustard.Saving.Editor;
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

        protected override void AddEditableFieldsToSubCardView(Box subCardView)
        {
            if (saveable is not Wallet wallet) { return; }
            if (!wallet.TryManualGetDataFromState(saveState, out WalletSaveData saveData))
            {
                subCardView.Add(new Label("No Wallet save data available"));
                return;
            }

            int cash = saveData.cash;
            int pendingCash = saveData.pendingCash;

            var cashRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(cashRow);
            cashRow.Add(new Label("Cash:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            var cashField = new IntegerField { value = cash, isDelayed = true, style = { flexGrow = 1 } };
            cashRow.Add(cashField);

            var pendingCashRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            subCardView.Add(pendingCashRow);
            pendingCashRow.Add(new Label("Pending Cash:") { style = { width = 120, unityTextAlign = TextAnchor.MiddleLeft } });
            var pendingCashField = new IntegerField { value = pendingCash, isDelayed = true, style = { flexGrow = 1 } };
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
