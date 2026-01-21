using UnityEngine;
using Frankie.Core;
using Frankie.Control;
using Frankie.Stats;
using Frankie.World;
using Frankie.Utils.UI;


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
        PlayerStateMachine playerStateMachine = null;
        PlayerController playerController = null;
        PartyKnapsackConduit partyKnapsackConduit = null;
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
                playerStateMachine?.EnterWorld();
            }
        }

        private void GetPlayerReference()
        {
            worldCanvas = WorldCanvas.FindWorldCanvas();
            playerStateMachine = Player.FindPlayerStateMachine();
            if (worldCanvas == null || playerStateMachine == null) { Destroy(gameObject); }

            partyKnapsackConduit = playerStateMachine.GetComponent<PartyKnapsackConduit>();
            playerController = playerStateMachine?.GetComponent<PlayerController>();
            shopper = playerStateMachine?.GetComponent<Shopper>();
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
            shopBox.Setup(worldCanvas, playerStateMachine, playerController, partyKnapsackConduit, shopper);
            Destroy(gameObject);
        }

        public void SpawnSellScreen() // Called by Unity Events
        {
            exitShopOnDestroy = false; // Shop exit to be called by child UI

            InventoryShopBox inventoryShopBox = Instantiate(inventoryShopBoxPrefab, worldCanvas.transform);
            inventoryShopBox.Setup(playerController, playerStateMachine, partyKnapsackConduit.GetComponent<PartyCombatConduit>(), shopper, shop.GetMessageForSale(), shop.GetMessageCannotSell());
            Destroy(gameObject);
        }
    }
}
