using Frankie.Speech.UI;
using Frankie.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory.UI
{
    public class InventoryItemField : UIChoiceOption
    {
        // Tunables
        [SerializeField] GameObject equippedMarker = null;

        // State
        IUIItemHandler uiItemHandler = null;
        Action<int> action = null;
        int value = -1;

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

        public bool HasAction()
        {
            if (action != null) { return true; }
            return false;
        }

        public void SetupButtonAction(IUIItemHandler uiItemHandler, Action<int> action, int value)
        {
            this.uiItemHandler = uiItemHandler;
            ListenToUIBoxState(true);

            this.action = action;
            this.value = value;
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
            if (inventoryBoxState == InventoryBoxState.inKnapsack || inventoryBoxState == InventoryBoxState.inCharacterSelection)
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
            if (equipmentBoxState == EquipmentBoxState.inEquipmentSelection || equipmentBoxState == EquipmentBoxState.inCharacterSelection)
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
