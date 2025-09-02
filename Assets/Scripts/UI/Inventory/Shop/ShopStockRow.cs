using Frankie.Utils.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Frankie.Inventory.UI
{
    public class ShopStockRow : UIChoiceButton
    {
        // Tunables
        [Header("Shop Specific Details")]
        [SerializeField] TMP_Text priceField = null;

        public void Setup(string inventoryItemName, int choiceOrder, int price, UnityAction purchaseAction)
        {
            SetText(inventoryItemName);
            SetChoiceOrder(choiceOrder);
            priceField.text = price.ToString();
            AddOnClickListener(purchaseAction);
        }
    }

}