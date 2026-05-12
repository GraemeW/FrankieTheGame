using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Core;
using Frankie.Control;
using Frankie.Stats;
using Frankie.Utils.Localization;
using Frankie.World;
using Frankie.Utils.UI;

namespace Frankie.Inventory.UI
{
    public class ShopSelect : UIBox, ILocalizable
    {
        [Header("Text")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageIntro;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionBuy;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionSell;
        [Header("Hookups")]
        [SerializeField] private TMP_Text introTextField;
        [SerializeField] private UIChoice choiceBuy;
        [SerializeField] private UIChoice choiceSell;
        
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
            
            if (introTextField != null) { introTextField.SetText(localizedMessageIntro.GetSafeLocalizedString()); }
            if (choiceBuy != null) { choiceBuy.SetText(localizedOptionBuy.GetLocalizedString()); }
            if (choiceSell != null) { choiceSell.SetText(localizedOptionSell.GetLocalizedString()); }

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
        
        #region LocalizationMethods

        public LocalizationTableType localizationTableType { get; } = LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            throw new System.NotImplementedException();
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
