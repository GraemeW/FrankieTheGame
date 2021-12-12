using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frankie.Utils.UI;
using TMPro;
using Frankie.Control;
using Frankie.Stats;
using Frankie.Speech.UI;

namespace Frankie.Inventory.UI
{
    public class ShopBox : UIBox
    {
        // Tunables
        [Header("Shop Specific Details")]
        [SerializeField] TMP_Text shopInfoField = null;
        [Header("Prefabs")]
        [SerializeField] ShopStockRow stockRowPrefab = null;
        [SerializeField] WalletUI walletUIPrefab = null;
        [SerializeField] InventoryShopBox inventoryShopBoxPrefab = null;
        [SerializeField] DialogueBox dialogueBoxPrefab = null;

        // State
        WalletUI walletUI = null;

        // Cached Reference
        WorldCanvas worldCanvas = null;
        PlayerStateHandler playerStateHandler = null;
        PlayerController playerController = null;
        Party party = null;
        PartyKnapsackConduit partyKnapsackConduit = null;
        Shopper shopper = null;
        Wallet wallet = null;
        Shop shop = null;

        #region UnityMethods
        private void Start()
        {
            SetupWalletUI();
        }

        private void SetupWalletUI()
        {
            walletUI = Instantiate(walletUIPrefab, worldCanvas.transform);
        }

        private void OnDestroy()
        {
            if (walletUI != null) { Destroy(walletUI.gameObject); }
            playerStateHandler?.ExitShop();
        }
        #endregion

        #region PublicMethods
        public void Setup(WorldCanvas worldCanvas, PlayerStateHandler playerStateHandler, PlayerController playerController, Shopper shopper)
        {
            this.worldCanvas = worldCanvas;
            this.playerStateHandler = playerStateHandler;
            this.playerController = playerController;
            this.shopper = shopper;

            SetupShopBox();
            party = playerStateHandler.GetParty();
            partyKnapsackConduit = party.GetComponent<PartyKnapsackConduit>();
            wallet = shopper.GetWallet();

            TakeControl(playerController, this, null); // input handled via player controller, immediate override
            HandleClientEntry();
        }

        public void UpdateShopMessage(string message)
        {
            shopInfoField.text = message;
        }

        public void UpdateShopMessageToSuccess()
        {
            shopInfoField.text = shop.GetMessageSuccess();
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
                ShopStockRow stockRow = Instantiate(stockRowPrefab, optionParent);
                stockRow.Setup(inventoryItem.GetDisplayName(), itemIndex, inventoryItem.GetPrice(), delegate { TryPurchaseItem(inventoryItem); });
                itemIndex++;
            }
            SetUpChoiceOptions();
        }

        private void TryPurchaseItem(InventoryItem inventoryItem)
        {
            if (wallet.GetCash() < inventoryItem.GetPrice())
            {
                SpawnMessage(shop.GetMessageNoFunds());
            }
            else if (!partyKnapsackConduit.HasFreeSpace())
            {
                SpawnMessage(shop.GetMessageNoSpace());
            }
            else
            {
                SpawnInventoryShopBox(inventoryItem);
            }
        }

        private void SpawnMessage(string message)
        {
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, worldCanvas.transform);
            dialogueBox.AddText(message);
            PassControl(dialogueBox);
        }

        private void SpawnInventoryShopBox(InventoryItem inventoryItem)
        {
            InventoryShopBox inventoryShopBox = Instantiate(inventoryShopBoxPrefab, worldCanvas.transform);
            inventoryShopBox.Setup(playerController, party, this, shopper, inventoryItem, shop.GetMessageNoSpace());
            PassControl(inventoryShopBox);
        }
        #endregion
    }
}