using Frankie.Control;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory
{
    public class Shop : MonoBehaviour
    {
        // Tunables
        [Header("Base Attributes")]
        [SerializeField] InventoryItem[] stock = null;
        [SerializeField] ShopType shopType = ShopType.Both;
        [SerializeField] float saleDiscount = 0.7f;
        [Header("Interaction Texts")]
        [SerializeField] string messageIntro = "What'cha want to buy?";
        [SerializeField] string messageSuccess = "Thanks, want anything else?";
        [SerializeField] string messageNoFunds = "Whoops, looks like you don't have enough cash for that";
        [SerializeField] string messageNoSpace = "Whoops, looks like you don't have enough space for that";
        [Tooltip("{0} for sale amount")][SerializeField] string messageForSale = "I can give you {0} for that";
        [SerializeField] string messageCannotSell = "I can't accept that item";

        public void InitiateBargain(PlayerStateHandler playerStateHandler) // Called via Unity events
        {
            if (stock == null) { return; }
            playerStateHandler.EnterShop(this);
        }

        public string GetMessageIntro()
        {
            return messageIntro;
        }

        public string GetMessageSuccess()
        {
            return messageSuccess;
        }

        public string GetMessageNoFunds()
        {
            return messageNoFunds;
        }

        public string GetMessageNoSpace()
        {
            return messageNoSpace;
        }

        public string GetMessageForSale()
        {
            return messageForSale;
        }

        public string GetMessageCannotSell()
        {
            return messageCannotSell;
        }

        public bool HasInventory()
        {
            return stock.Length > 0;
        }

        public IEnumerable<InventoryItem> GetShopStock()
        {
            foreach (InventoryItem inventoryItem in stock)
            {
                yield return inventoryItem;
            }
        }

        public ShopType GetShopType()
        {
            return shopType;
        }

        public float GetSaleDiscount()
        {
            return saleDiscount;
        }
    }
}