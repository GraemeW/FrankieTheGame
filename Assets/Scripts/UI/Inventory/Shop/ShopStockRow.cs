using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Frankie.Utils.UI;

namespace Frankie.Inventory.UI
{
    public class ShopStockRow : UIChoiceButton
    {
        // Tunables
        [Header("Shop Specific Details")]
        [SerializeField] private TMP_Text priceField;

        public void Setup(string inventoryItemName, int setChoiceOrder, int price, UnityAction purchaseAction)
        {
            SetText(inventoryItemName);
            SetChoiceOrder(setChoiceOrder);
            priceField.text = price.ToString();
            AddOnClickListener(purchaseAction);
        }
    }
}
