using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils.UI;
using Frankie.Control;
using Frankie.Speech.UI;
using Frankie.Utils;

namespace Frankie.Inventory.UI
{
    public class ShopSelect : UIBox
    {
        // Prefabs
        [Header("Shop Prefabs")]
        [SerializeField] ShopBox shopBoxPrefab = null;
        //TODO:  Add inventoryShopBox for selling

        // Cached Reference
        PlayerStateHandler playerStateHandler = null;
        PlayerController playerController = null;
        WorldCanvas worldCanvas = null;
        Shopper shopper = null;
        Shop shop = null;

        private void Awake()
        {
            GetPlayerReference();
        }

        private void GetPlayerReference()
        {
            worldCanvas = GameObject.FindGameObjectWithTag("WorldCanvas")?.GetComponent<WorldCanvas>();
            playerStateHandler = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerStateHandler>();
            if (worldCanvas == null || playerStateHandler == null) { Destroy(gameObject); }

            playerController = playerStateHandler?.GetComponent<PlayerController>();
            shopper = playerStateHandler?.GetComponent<Shopper>();
        }

        private void Start()
        {
            TakeControl(playerController, this, null); // input handled via player controller, immediate override
            HandleClientEntry();

            shop = shopper.GetCurrentShop();
            if (shop == null) { Destroy(gameObject); }

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
            ShopBox shopBox = Instantiate(shopBoxPrefab, worldCanvas.gameObject.transform);
            shopBox.Setup(worldCanvas, playerStateHandler, playerController, shopper);
            Destroy(gameObject);
        }

        public void SpawnSellScreen() // Called by Unity Events
        {
            //TODO:  Implement
            Destroy(gameObject);
        }
    }
}