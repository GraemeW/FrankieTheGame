using Frankie.Speech.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory.UI
{
    public class InventoryItemField : DialogueChoiceOption
    {
        // State
        InventoryBox inventoryBox = null;
        Action<int> action = null;
        int value = -1;

        private void OnEnable()
        {
            ToggleButtonActive(true);
        }

        private void OnDisable()
        {
            ToggleButtonActive(false);
        }

        public void SetupButtonAction(InventoryBox inventoryBox, Action<int> action, int value)
        {
            this.inventoryBox = inventoryBox;
            inventoryBox.inventoryBoxStateChanged += HandleInventoryBoxStateChange;

            this.action = action;
            this.value = value;
            ToggleButtonActive(true);
        }

        private void ToggleButtonActive(bool enable)
        {
            if (action == null) { return; }

            if (enable)
            {
                button.onClick.AddListener(delegate { action.Invoke(value); });
            }
            else
            {
                button.onClick.RemoveAllListeners();
            }
        }

        private void HandleInventoryBoxStateChange(InventoryBoxState inventoryBoxState)
        {
            if (inventoryBoxState == InventoryBoxState.inKnapsack || inventoryBoxState == InventoryBoxState.inCharacterSelection)
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
