using System;
using UnityEngine;
using Frankie.Utils.UI;

namespace Frankie.Inventory.UI
{
    public class InventoryItemField : UIChoiceButton
    {
        // Tunables
        [SerializeField] private GameObject equippedMarker;

        // State
        private IUIItemHandler uiItemHandler;
        private Action<int> action;
        private int value = -1;

        private void OnEnable()
        {
            ToggleButtonActive(true);
            ListenToUIBoxState(true);
            if (uiItemHandler != null) {  }
        }

        private void OnDisable()
        {
            ToggleButtonActive(false);
            ListenToUIBoxState(false);
            if (uiItemHandler != null) { }
        }

        public bool HasAction() => action != null;

        public void SetupButtonAction(IUIItemHandler setUIItemHandler, Action<int> setAction, int setValue)
        {
            uiItemHandler = setUIItemHandler;
            ListenToUIBoxState(true);

            action = setAction;
            value = setValue;
            ToggleButtonActive(true);
        }

        public void SetEquipped(bool enable)
        {
            equippedMarker.SetActive(enable);
        }

        private void ListenToUIBoxState(bool enable)
        {
            if (uiItemHandler == null) { return; }
            
            if (enable)
            {
                if (uiItemHandler.GetType() == typeof(InventoryBox))
                {
                    uiItemHandler.uiBoxStateChanged += HandleInventoryBoxStateChange;
                }
                else if (uiItemHandler.GetType() == typeof(EquipmentBox))
                {
                    uiItemHandler.uiBoxStateChanged += HandleEquipmentBoxStateChange;
                }
            }
            else
            {
                if (uiItemHandler.GetType() == typeof(InventoryBox))
                {
                    uiItemHandler.uiBoxStateChanged -= HandleInventoryBoxStateChange;
                }
                else if (uiItemHandler.GetType() == typeof(EquipmentBox))
                {
                    uiItemHandler.uiBoxStateChanged -= HandleEquipmentBoxStateChange;
                }
            }
        }

        private void ToggleButtonActive(bool enable)
        {
            if (action == null) { return; }
            button.onClick.RemoveAllListeners();

            if (enable)
            {
                button.onClick.AddListener(delegate { action.Invoke(value); });
            }
        }

        private void HandleInventoryBoxStateChange(Enum uiBoxState)
        {
            InventoryBoxState inventoryBoxState = (InventoryBoxState)uiBoxState;
            if (inventoryBoxState == InventoryBoxState.InKnapsack || inventoryBoxState == InventoryBoxState.InCharacterSelection)
            {
                ToggleButtonActive(true);
            }
            else
            {
                ToggleButtonActive(false);
            }
        }

        private void HandleEquipmentBoxStateChange(Enum uiBoxState)
        {
            EquipmentBoxState equipmentBoxState = (EquipmentBoxState)uiBoxState;
            if (equipmentBoxState == EquipmentBoxState.InEquipmentSelection || equipmentBoxState == EquipmentBoxState.InCharacterSelection)
            {
                ToggleButtonActive(true);
            }
            else
            {
                ToggleButtonActive(false);
            }
        }
    }
}
