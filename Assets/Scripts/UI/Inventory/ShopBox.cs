using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils.UI;
using TMPro;
using Frankie.Control;

namespace Frankie.Inventory.UI
{
    public class ShopBox : UIBox
    {
        // Tunables
        [Header("Shop Specific Details")]
        [SerializeField] TMP_Text shopInfoField = null;
        [Header("Prefabs")]
        [SerializeField] ShopStockRow stockRowPrefab = null;

        // Cached Reference
        PlayerStateHandler playerStateHandler = null;
        PlayerController playerController = null;
        WorldCanvas worldCanvas = null;
        Shopper shopper = null;
        Shop shop = null;

        #region UnityMethods
        private void Awake()
        {
            GetPlayerReference();
        }

        private void Start()
        {
            SetupShopBox();

            TakeControl(playerController, this, null); // input handled via player controller, immediate override
            HandleClientEntry();
        }

        private void OnDestroy()
        {
            playerStateHandler?.ExitShop();
        }
        #endregion

        #region PrivateMethods
        private void GetPlayerReference()
        {
            worldCanvas = GameObject.FindGameObjectWithTag("WorldCanvas")?.GetComponent<WorldCanvas>();
            playerStateHandler = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerStateHandler>();
            if (worldCanvas == null || playerStateHandler == null) { Destroy(gameObject); }

            playerController = playerStateHandler?.GetComponent<PlayerController>();
            shopper = playerStateHandler?.GetComponent<Shopper>();
        }

        private void SetupShopBox()
        {
            shop = shopper.GetCurrentShop();
            if (shop == null) { Destroy(gameObject); }

            shopInfoField.text = shop.GetMessageIntro();

            ClearChoiceSelections();
            int itemIndex = 0;
            foreach (InventoryItem inventoryItem in shop.GetShopStock())
            {
                ShopStockRow stockRow = Instantiate(stockRowPrefab, optionParent);
                stockRow.Setup(inventoryItem.GetDisplayName(), itemIndex, inventoryItem.GetPrice(), delegate { TryPurchaseItem(inventoryItem); });
                itemIndex++;
            }
            SetUpChoiceOptions();
        }

        private void TryPurchaseItem(InventoryItem inventoryItem)
        {

        }
        #endregion
    }
}