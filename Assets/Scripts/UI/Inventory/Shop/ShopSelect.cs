using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.Localization;
using Frankie.Core;
using Frankie.Control;
using Frankie.Stats;
using Frankie.World;
using Frankie.Utils.UI;

namespace Frankie.Inventory.UI
{
    public class ShopSelect : UIBox<UIBoxState>, ILocalizable
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
        protected override bool TryAcquireDependencies() => GetPlayerReference();

        protected override void AwakeTriggered()
        {
            clearVolatileOptionsOnEnable = false;
        }

        protected override void StartTriggered()
        {
            shop = shopper.GetCurrentShop();
            if (shop == null || !shop.HasInventory()) { Destroy(gameObject); return; }
            
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

        protected override void DestroyTriggered()
        {
            if (exitShopOnDestroy && playerStateMachine != null) { playerStateMachine.EnterWorld(); }
        }
        
        private bool GetPlayerReference()
        {
            worldCanvas = WorldCanvas.FindWorldCanvas();
            playerStateMachine = Player.FindPlayerStateMachine();
            if (worldCanvas == null || playerStateMachine == null) { return false; }

            partyKnapsackConduit = playerStateMachine.GetComponent<PartyKnapsackConduit>();
            playerController = playerStateMachine.GetComponent<PlayerController>();
            shopper = playerStateMachine.GetComponent<Shopper>();
            if (playerController == null) { return false; }
            
            playerController.AddInputReceiver(this, null);
            return true;
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
            inventoryShopBox.Setup(playerController, playerStateMachine, partyKnapsackConduit.GetComponent<PartyCombatConduit>(), shopper, shop);
            Destroy(gameObject);
        }
        #endregion
    }
}
