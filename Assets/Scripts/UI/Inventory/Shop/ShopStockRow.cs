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
