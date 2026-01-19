using UnityEngine;
using TMPro;
using Frankie.Utils.UI;
using Frankie.Control;
using Frankie.Stats;
using Frankie.World;
using Frankie.Speech.UI;

namespace Frankie.Inventory.UI
{
    public class ShopBox : UIBox
    {
        // Tunables
        [Header("Shop Specific Details")]
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

            HandleClientExit();
            playerStateMachine?.EnterWorld();
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

            TakeControl(setPlayerController, this, null); // input handled via player controller, immediate override
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
            if (wallet.GetCash() < inventoryItem.GetPrice()) { SpawnMessage(shop.GetMessageNoFunds()); }
            else if (!partyKnapsackConduit.HasFreeSpace()) { SpawnMessage(shop.GetMessageNoSpace()); }
            else { SpawnInventoryShopBox(inventoryItem); }
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
            inventoryShopBox.Setup(playerController, partyKnapsackConduit.GetComponent<PartyCombatConduit>(), shopper, this, inventoryItem, shop.GetMessageNoSpace());
            PassControl(inventoryShopBox);
        }
        #endregion
    }
}
