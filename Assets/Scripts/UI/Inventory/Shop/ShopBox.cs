using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.UIBox;
using LowDefMustard.Localization;
using Frankie.Core;
using Frankie.Control;
using Frankie.Stats;
using Frankie.World;
using Frankie.Speech.UI;
using Frankie.Utils.Localization;

namespace Frankie.Inventory.UI
{
    [RequireComponent(typeof(UIToolkitMenuView))]
    public sealed class ShopBox : UIBox<UIBoxState>, ILocalizable
    {
        // Tunables
        [Header("Shop Specific Details")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedShopInfoDefault;
        [Header("Prefabs")]
        [SerializeField] private WalletUI walletUIPrefab;
        [SerializeField] private InventoryShopBox inventoryShopBoxPrefab;
        [SerializeField] private DialogueBox dialogueBoxPrefab;

        // State
        private readonly ShopMessageModel shopMessageModel = new();
        private WalletUI walletUI;
        private InventoryShopBox activeInventoryShopBox;

        // Cached Reference
        private UIToolkitMenuView menuView;
        private WorldCanvas worldCanvas;
        private PlayerStateMachine playerStateMachine;
        private PlayerController playerController;
        private PartyKnapsackConduit partyKnapsackConduit;
        private Shopper shopper;
        private Wallet wallet;
        private Shop shop;

        #region UnityMethods
        protected override void AwakeTriggered()
        {
            clearVolatileOptionsOnEnable = false;
            menuView = GetComponent<UIToolkitMenuView>();
            menuView.SetDataSource(shopMessageModel);
        }

        protected override void StartTriggered()
        {
            walletUI = Instantiate(walletUIPrefab, worldCanvas.transform);
            UpdateShopMessage(localizedShopInfoDefault.GetSafeLocalizedString());
        }

        protected override void DestroyTriggered()
        {
            if (walletUI != null) { Destroy(walletUI.gameObject); }
            playerStateMachine?.EnterWorld();
        }
        #endregion

        #region LocalizationMethods
        public LocalizationTableType localizationTableType { get; } =  LocalizationTableType.UI;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedShopInfoDefault.TableEntryReference,
            };
        }
        #endregion

        #region PublicMethods
        public void Setup(WorldCanvas setWorldCanvas, PlayerStateMachine setPlayerStateMachine, PlayerController setPlayerController, PartyKnapsackConduit setPartyKnapsackConduit, Shopper setShopper)
        {
            if (setPlayerController == null) { destroyQueued = true; return; }

            worldCanvas = setWorldCanvas;
            playerStateMachine = setPlayerStateMachine;
            playerController = setPlayerController;
            shopper = setShopper;

            SetupShopBox();
            partyKnapsackConduit = setPartyKnapsackConduit;
            wallet = setShopper.GetWallet();

            playerController.AddInputReceiver(this, null);
        }

        private void UpdateShopMessage(string message) => shopMessageModel.messageText = message;

        public void UpdateShopMessageToSuccess()
        {
            if (shop == null) { return; }
            UpdateShopMessage(shop.GetMessageSuccess());
        }
        #endregion

        #region PrivateMethods
        private void SetupShopBox()
        {
            shop = shopper.GetCurrentShop();
            if (shop == null) { destroyQueued = true; return; }

            UpdateShopMessage(shop.GetMessageIntro());

            foreach (InventoryItem inventoryItem in shop.GetShopStock())
            {
                if (inventoryItem == null)  { continue; }

                var shopStockModel = new ShopStockModel
                {
                    itemName = inventoryItem.GetDisplayName(),
                    priceText = inventoryItem.GetPrice().ToString(CultureInfo.InvariantCulture)
                };
                var shopStockHandle = new ShopStockHandle(menuView, shopStockModel, () => StandardChoiceExecution(() => TryPurchaseItem(inventoryItem), false));
                menuView.AddEntry(shopStockHandle);
                choiceOptions.Add(shopStockHandle);
            }
            ReconcileChoiceOptions();
        }

        private void TryPurchaseItem(InventoryItem inventoryItem)
        {
            if (wallet.GetCash() < inventoryItem.GetPrice()) { SpawnMessage(shop.GetMessageNoFunds()); }
            else if (!partyKnapsackConduit.HasFreeSpace()) { SpawnMessage(shop.GetMessageNoSpace()); }
            else
            {
                if (activeInventoryShopBox != null) { Destroy(activeInventoryShopBox.gameObject); }
                activeInventoryShopBox = SpawnInventoryShopBox(inventoryItem);
            }
        }

        private void SpawnMessage(string message)
        {
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, worldCanvas.transform);
            dialogueBox.AddText(message);
            controller.AddInputReceiver(dialogueBox, null);
        }

        private InventoryShopBox SpawnInventoryShopBox(InventoryItem inventoryItem)
        {
            InventoryShopBox inventoryShopBox = Instantiate(inventoryShopBoxPrefab, worldCanvas.transform);
            inventoryShopBox.Setup(playerController, partyKnapsackConduit.GetComponent<PartyCombatConduit>(), shopper, shop, this, inventoryItem);
            controller.AddInputReceiver(inventoryShopBox, null);
            return inventoryShopBox;
        }
        #endregion
    }
}
