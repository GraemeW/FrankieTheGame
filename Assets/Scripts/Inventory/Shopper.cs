using System;
using UnityEngine;

namespace Frankie.Inventory
{
    [RequireComponent(typeof(Wallet))]
    public class Shopper : MonoBehaviour
    {
        // State
        Shop currentShop = null;
        BankType bankType = BankType.None;
        
        // Cached References
        Wallet wallet = null;

        // Events
        public event Action transactionCompleted;

        private void Awake()
        {
            wallet = GetComponent<Wallet>();
        }

        public void SetShop(Shop shop)
        {
            currentShop = shop;
        }

        public void SetBankType(BankType bankType)
        {
            this.bankType = bankType;
        }

        public Shop GetCurrentShop()
        {
            return currentShop;
        }

        public BankType GetBankType()
        {
            return bankType;
        }

        public Wallet GetWallet()
        {
            return wallet;
        }

        public void CompleteTransaction(ShopType saleType, InventoryItem inventoryItem, Knapsack characterKnapsack)
        {
            if (currentShop == null) { return; }
            if (saleType == ShopType.Both) { return; } // Invalid call

            int transactionFee = inventoryItem.GetPrice();
            if (saleType == ShopType.Sell)
            {
                transactionFee = Mathf.RoundToInt(transactionFee * currentShop.GetSaleDiscount());
            }
            if (saleType == ShopType.Buy)
            {
                transactionFee *= -1;
            }

            wallet.UpdateCash(transactionFee);

            if (saleType == ShopType.Buy)
            {
                characterKnapsack.AddToFirstEmptySlot(inventoryItem, true);
            }
            else if (saleType == ShopType.Sell)
            {
                characterKnapsack.RemoveItem(inventoryItem, true);
            }

            transactionCompleted?.Invoke();
        }
    }
}
