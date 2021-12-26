using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils.UI;
using Frankie.Control;
using Frankie.Stats;

namespace Frankie.Inventory.UI
{
    public class ShopSelect : UIBox
    {
        // Prefabs
        [Header("Shop Prefabs")]
        [SerializeField] ShopBox shopBoxPrefab = null;
        [SerializeField] InventoryShopBox inventoryShopBoxPrefab = null;

        // Bool
        bool exitShopOnDestroy = true;

        // Cached Reference
        WorldCanvas worldCanvas = null;
        PlayerStateHandler playerStateHandler = null;
        PlayerController playerController = null;
        Party party = null;
        Shopper shopper = null;
        Shop shop = null;

        private void Awake()
        {
            GetPlayerReference();
        }

        private void OnDestroy()
        {
            if (exitShopOnDestroy)
            {
                playerStateHandler?.ExitShop();
            }
        }

        private void GetPlayerReference()
        {
            worldCanvas = GameObject.FindGameObjectWithTag("WorldCanvas")?.GetComponent<WorldCanvas>();
            playerStateHandler = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerStateHandler>();
            if (worldCanvas == null || playerStateHandler == null) { Destroy(gameObject); }

            party = playerStateHandler.GetParty();
            playerController = playerStateHandler?.GetComponent<PlayerController>();
            shopper = playerStateHandler?.GetComponent<Shopper>();
        }

        private void Start()
        {
            TakeControl(playerController, this, null); // input handled via player controller, immediate override
            HandleClientEntry();

            shop = shopper.GetCurrentShop();
            if (shop == null || !shop.HasInventory()) { Destroy(gameObject); }

            ShopType shopType = shop.GetShopType();
            if (shopType == ShopType.Buy)
            {
                SpawnBuyScreen();
            }
            else if (shopType == ShopType.Sell)
            {
                SpawnSellScreen();
            }
            // Otherwise Both -> standard menu interaction, configured in Unity
        }

        public void SpawnBuyScreen() // Called by Unity Events
        {
            exitShopOnDestroy = false; // Shop exit to be called by child UI

            ShopBox shopBox = Instantiate(shopBoxPrefab, worldCanvas.transform);
            shopBox.Setup(worldCanvas, playerStateHandler, playerController, party, shopper);
            Destroy(gameObject);
        }

        public void SpawnSellScreen() // Called by Unity Events
        {
            exitShopOnDestroy = false; // Shop exit to be called by child UI

            InventoryShopBox inventoryShopBox = Instantiate(inventoryShopBoxPrefab, worldCanvas.transform);
            inventoryShopBox.Setup(playerController, playerStateHandler, party, shopper, shop.GetMessageForSale(), shop.GetMessageCannotSell());
            Destroy(gameObject);
        }
    }
}