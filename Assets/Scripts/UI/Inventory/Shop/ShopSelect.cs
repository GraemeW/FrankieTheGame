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
        [SerializeField] private ShopBox shopBoxPrefab;
        [SerializeField] private InventoryShopBox inventoryShopBoxPrefab;

        // Bool
        private bool exitShopOnDestroy = true;

        // Cached Reference
        private WorldCanvas worldCanvas;
        private PlayerStateMachine playerStateMachine;
        private PlayerController playerController;
        private PartyKnapsackConduit partyKnapsackConduit;
        private Shopper shopper;
        private Shop shop;

        #region UnityMethods
        private void Awake()
        {
            GetPlayerReference();
        }
        
        private void Start()
        {
            // Input handled via player controller, immediate override
            TakeControl(playerController, this, null); 
            HandleClientEntry();

            shop = shopper.GetCurrentShop();
            if (shop == null || !shop.HasInventory()) { Destroy(gameObject); }

            ShopType shopType = shop.GetShopType();
            switch (shopType)
            {
                case ShopType.Buy:
                    SpawnBuyScreen();
                    break;
                case ShopType.Sell:
                    SpawnSellScreen();
                    break;
            }
            // Otherwise Both -> standard menu interaction, configured in Unity
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
            playerController = playerStateMachine.GetComponent<PlayerController>();
            shopper = playerStateMachine.GetComponent<Shopper>();
        }
        #endregion

        #region PublicMethods
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
        #endregion
    }
}
