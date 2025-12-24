using System;
using UnityEngine;

namespace Frankie.Inventory
{
    [RequireComponent(typeof(Wallet))]
    public class Shopper : MonoBehaviour
    {
        // State
        private Shop currentShop;
        private BankType bankType = BankType.None;
        
        // Cached References
        private Wallet wallet;

        // Events
        public event Action transactionCompleted;

        #region UnityMethods
        private void Awake()
        {
            wallet = GetComponent<Wallet>();
        }
        #endregion

        #region GettersSetters
        public Shop GetCurrentShop() => currentShop;
        public BankType GetBankType() => bankType;
        public Wallet GetWallet() => wallet;
        public void SetShop(Shop shop)
        {
            currentShop = shop;
        }

        public void SetBankType(BankType setBankType)
        {
            bankType = setBankType;
        }
        #endregion

        public void CompleteTransaction(ShopType saleType, InventoryItem inventoryItem, Knapsack characterKnapsack)
        {
            if (currentShop == null) { return; }
            if (saleType == ShopType.Both) { return; } // Invalid call

            int transactionFee = inventoryItem.GetPrice();
            switch (saleType)
            {
                case ShopType.Sell:
                    transactionFee = Mathf.RoundToInt(transactionFee * currentShop.GetSaleDiscount());
                    break;
                case ShopType.Buy:
                    transactionFee *= -1;
                    break;
            }

            wallet.UpdateCash(transactionFee);

            switch (saleType)
            {
                case ShopType.Buy:
                    characterKnapsack.AddToFirstEmptySlot(inventoryItem, true);
                    break;
                case ShopType.Sell:
                    characterKnapsack.RemoveItem(inventoryItem, true);
                    characterKnapsack.SquishItemsInKnapsack();
                    break;
            }
            transactionCompleted?.Invoke();
        }
    }
}
