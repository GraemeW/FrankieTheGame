using System.Collections.Generic;
using UnityEngine;
using Frankie.Combat;
using Frankie.Control;
using Frankie.Speech.UI;
using Frankie.Stats;
using Frankie.Utils;

namespace Frankie.Inventory.UI
{
    public class InventoryShopBox : InventoryBox
    {
        // Tunables
        [Header("Shop Specific")]
        [SerializeField] private string messageOptionSell = "Sell";
        [SerializeField] private string messageOptionCancelSale = "On second thought...";
        [SerializeField] private WalletUI walletUIPrefab;

        // State
        private ShopType transactionType = ShopType.Both;
        private InventoryItem buyItem;
        private string messageNoSpace = "";
        private string messageForSale = "";
        private string messageCannotSell = "";

        // Cached References
        private PlayerStateMachine playerStateMachine;
        private WalletUI walletUI;
        private ShopBox shopBox;
        private Shopper shopper;

        #region Initialization
        // Buy-specific
        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, PartyCombatConduit partyCombatConduit, Shopper setShopper, ShopBox setShopBox, InventoryItem setBuyItem, string setMessageNoSpace)
        {
            transactionType = ShopType.Buy;
            
            base.Setup(standardPlayerInputCaller, partyCombatConduit);
            shopper = setShopper;
            shopBox = setShopBox;
            buyItem = setBuyItem;
            messageNoSpace = setMessageNoSpace;
        }

        // Sell-specific
        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, PlayerStateMachine setPlayerStateMachine, PartyCombatConduit partyCombatConduit, Shopper setShopper, string setMessageForSale, string setMessageCannotSell)
        {
            transactionType = ShopType.Sell;

            base.Setup(standardPlayerInputCaller, partyCombatConduit);
            playerStateMachine = setPlayerStateMachine;
            shopper = setShopper;
            messageForSale = setMessageForSale;
            messageCannotSell = setMessageCannotSell;

            SetupWalletUI();
            TakeControl(standardPlayerInputCaller, this, null); // input handled via player controller, immediate override
            HandleClientEntry();
        }

        private void SetupWalletUI()
        {
            walletUI = Instantiate(walletUIPrefab, transform.parent);
        }

        private void OnDestroy()
        {
            if (shopBox != null) { shopBox.UpdateShopMessageToSuccess(); }
            if (walletUI != null) { Destroy(walletUI.gameObject); }

            HandleClientExit();
            if (transactionType == ShopType.Sell) { playerStateMachine?.EnterWorld(); }
        }
        #endregion

        #region BuySpecificOverrides
        protected override void ChooseCharacter(CombatParticipant character, bool initializeCursor = true)
        {
            switch (transactionType)
            {
                case ShopType.Buy:
                {
                    UpdateKnapsackView(character);
                    SetInventoryBoxState(InventoryBoxState.inCharacterSelection);

                    Knapsack characterKnapsack = selectedCharacter.GetComponent<Knapsack>();
                    Knapsack selectedCharacterKnapsack = selectedCharacter.GetComponent<Knapsack>();
                    if (characterKnapsack == null || selectedCharacterKnapsack == null) { return; }

                    if (selectedCharacterKnapsack.HasFreeSpace())
                    {
                        shopper.CompleteTransaction(ShopType.Buy, buyItem, characterKnapsack);
                        Destroy(gameObject);
                    }
                    else
                    {
                        SpawnMessage(messageNoSpace);
                    }

                    break;
                }
                case ShopType.Sell:
                    base.ChooseCharacter(character, initializeCursor);
                    break;
            }
        }

        protected override void SoftChooseCharacter(CombatParticipant character)
        {
            switch (transactionType)
            {
                case ShopType.Buy:
                    UpdateKnapsackView(character);
                    break;
                case ShopType.Sell:
                    base.SoftChooseCharacter(character);
                    break;
            }
        }
        #endregion

        #region SellSpecificOverrides
        protected override List<ChoiceActionPair> GetChoiceActionPairs(int inventorySlot)
        {
            switch (transactionType)
            {
                case ShopType.Sell:
                {
                    // Guard against invalid entity
                    var choiceActionPairs = new List<ChoiceActionPair>();
                    if (selectedKnapsack == null) { return choiceActionPairs; }
                    InventoryItem inventoryItem = selectedKnapsack.GetItemInSlot(inventorySlot);
                    if (inventoryItem == null) { return choiceActionPairs; }
                    
                    // Sale
                    if (selectedCharacter.TryGetComponent(out Knapsack selectedCharacterKnapsack))
                    {
                        var sellActionPair = new ChoiceActionPair(messageOptionSell, () => shopper.CompleteTransaction(ShopType.Sell, inventoryItem, selectedCharacterKnapsack));
                        choiceActionPairs.Add(sellActionPair);
                    }
                    
                    // Cancel
                    var cancelActionPair = new ChoiceActionPair(messageOptionCancelSale, () => { });
                    choiceActionPairs.Add(cancelActionPair);

                    return choiceActionPairs;
                }
                case ShopType.Buy:
                    return base.GetChoiceActionPairs(inventorySlot);
                default:
                    return new List<ChoiceActionPair>();
            }
        }

        protected override void ChooseItem(int inventorySlot)
        {
            switch (transactionType)
            {
                case ShopType.Sell:
                {
                    // Check if item is sellable
                    InventoryItem inventoryItem = selectedKnapsack.GetItemInSlot(inventorySlot);
                    if (inventoryItem == null) { return; }

                    if (inventoryItem.GetType() == typeof(KeyItem)) { SpawnMessage(messageCannotSell); }
                    else { SpawnSellMenu(inventorySlot); }
                    
                    break;
                }
                case ShopType.Buy:
                    base.ChooseItem(inventorySlot);
                    break;
            }
        }

        private void SpawnSellMenu(int inventorySlot)
        {
            InventoryItem inventoryItem = selectedKnapsack.GetItemInSlot(inventorySlot);
            Shop shop = shopper.GetCurrentShop();
            if (inventoryItem == null || shop == null) { return; }

            int salePrice = Mathf.RoundToInt(inventoryItem.GetPrice() * shop.GetSaleDiscount());
            string saleMessage = string.Format(messageForSale, salePrice.ToString());

            List<ChoiceActionPair> choiceActionPairs = GetChoiceActionPairs(inventorySlot);
            if (choiceActionPairs == null || choiceActionPairs.Count == 0) { return; }

            DialogueOptionBox dialogueOptionBox = Instantiate(dialogueOptionBoxPrefab, transform.parent);
            dialogueOptionBox.Setup(saleMessage);
            dialogueOptionBox.OverrideChoiceOptions(choiceActionPairs);
            PassControl(dialogueOptionBox);
        }
        #endregion

        #region UtilityMethods
        private void SpawnMessage(string message)
        {
            DialogueBox dialogueBox = Instantiate(dialogueBoxPrefab, transform.parent);
            dialogueBox.AddText(message);
            PassControl(dialogueBox);
        }
        #endregion
    }
}
