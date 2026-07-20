using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using TMPro;
using Frankie.Core;
using Frankie.Control;
using Frankie.Stats;
using Frankie.World;
using Frankie.Utils.Localization;
using Frankie.Utils.UI;
using Frankie.Speech.UI;

namespace Frankie.Inventory.UI
{
    public class ShopBox : UIBox, ILocalizable
    {
        // Tunables
        [Header("Shop Specific Details")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedShopInfoDefault;
        [SerializeField] private TMP_Text shopInfoField;
        [Header("Prefabs")]
        [SerializeField] private ShopStockRow stockRowPrefab;
        [SerializeField] private WalletUI walletUIPrefab;
        [SerializeField] private InventoryShopBox inventoryShopBoxPrefab;
        [SerializeField] private DialogueBox dialogueBoxPrefab;

        // State
        private WalletUI walletUI;

        // Cached Reference
        private WorldCanvas worldCanvas;
        private PlayerStateMachine playerStateMachine;
        private PlayerController playerController;
        private PartyKnapsackConduit partyKnapsackConduit;
        private Shopper shopper;
        private Wallet wallet;
        private Shop shop;

        #region UnityMethods
        protected override void Start()
        {
            base.Start();
            walletUI = Instantiate(walletUIPrefab, worldCanvas.transform);
            if (shopInfoField != null) { shopInfoField.SetText(localizedShopInfoDefault.GetSafeLocalizedString()); }
        }

        protected override void OnDestroy()
        {
            if (walletUI != null) { Destroy(walletUI.gameObject); }

            base.OnDestroy();
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
            worldCanvas = setWorldCanvas;
            playerStateMachine = setPlayerStateMachine;
            playerController = setPlayerController;
            shopper = setShopper;

            SetupShopBox();
            partyKnapsackConduit = setPartyKnapsackConduit;
            wallet = setShopper.GetWallet();

            playerController.AddInputReceiver(this, null);
        }

        public void UpdateShopMessage(string message) // Callable via Unity Events
        {
            shopInfoField.text = message;
        }

        public void UpdateShopMessageToSuccess() // Callable via Unity Events
        {
            if (shop == null) { return; }
            UpdateShopMessage(shop.GetMessageSuccess());
        }
        #endregion

        #region PrivateMethods
        private void SetupShopBox()
        {
            shop = shopper.GetCurrentShop();
            if (shop == null) { Destroy(gameObject); }

            shopInfoField.text = shop.GetMessageIntro();

            ClearChoiceSelections();
            int itemIndex = 0;
            foreach (InventoryItem inventoryItem in shop.GetShopStock())
            {
                if (inventoryItem == null)  { continue; }
                
                ShopStockRow stockRow = Instantiate(stockRowPrefab, optionParent);
                stockRow.Setup(inventoryItem.GetDisplayName(), itemIndex, inventoryItem.GetPrice(), delegate { TryPurchaseItem(inventoryItem); });
                itemIndex++;
            }
            SetUpChoiceOptions();
        }

        private void TryPurchaseItem(InventoryItem inventoryItem)
        {
            if (wallet.GetCash() < inventoryItem.GetPrice()) { SpawnMessage(shop.GetMessageNoFunds()); }
            else if (!partyKnapsackConduit.HasFreeSpace()) { SpawnMessage(shop.GetMessageNoSpace()); }
            else { SpawnInventoryShopBox(inventoryItem); }
        }

        private void SpawnMessage(string message)
        {
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, worldCanvas.transform);
            dialogueBox.AddText(message);
            controller.AddInputReceiver(dialogueBox, null);
        }

        private void SpawnInventoryShopBox(InventoryItem inventoryItem)
        {
            InventoryShopBox inventoryShopBox = Instantiate(inventoryShopBoxPrefab, worldCanvas.transform);
            inventoryShopBox.Setup(playerController, partyKnapsackConduit.GetComponent<PartyCombatConduit>(), shopper, this, inventoryItem, shop.GetMessageNoSpace());
            controller.AddInputReceiver(inventoryShopBox, null);
        }
        #endregion
    }
}
