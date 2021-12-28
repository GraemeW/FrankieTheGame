using Frankie.Combat;
using Frankie.Control;
using Frankie.Speech.UI;
using Frankie.Stats;
using Frankie.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory.UI
{
    public class InventoryShopBox : InventoryBox
    {
        // Tunables
        [Header("Shop Specific")]
        [SerializeField] string messageOptionSell = "Sell";
        [SerializeField] string messageOptionCancelSale = "On second thought...";
        [SerializeField] WalletUI walletUIPrefab = null;

        // State
        ShopType transactionType = ShopType.Both;
        InventoryItem buyItem = null;
        string messageNoSpace = "";
        string messageForSale = "";
        string messageCannotSell = "";

        // Cached References
        PlayerStateHandler playerStateHandler = null;
        WalletUI walletUI = null;
        ShopBox shopBox = null;
        Shopper shopper = null;

        #region Initialization
        // Buy-specific
        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, Party party, Shopper shopper, ShopBox shopBox, InventoryItem buyItem, string messageNoSpace) 
        {
            transactionType = ShopType.Buy;

            base.Setup(standardPlayerInputCaller, party);
            this.shopper = shopper;
            this.shopBox = shopBox;
            this.buyItem = buyItem;
            this.messageNoSpace = messageNoSpace;
        }

        // Sell-specific
        public void Setup(IStandardPlayerInputCaller standardPlayerInputCaller, PlayerStateHandler playerStateHandler, Party party, Shopper shopper, string messageForSale, string messageCannotSell)
        {
            transactionType = ShopType.Sell;

            base.Setup(standardPlayerInputCaller, party);
            this.playerStateHandler = playerStateHandler;
            this.shopper = shopper;
            this.messageForSale = messageForSale;
            this.messageCannotSell = messageCannotSell;

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
            if (transactionType == ShopType.Sell) { playerStateHandler?.ExitShop(); }
        }
        #endregion

        #region BuySpecificOverrides
        protected override void ChooseCharacter(CombatParticipant character, bool initializeCursor = true)
        {
            if (transactionType == ShopType.Buy)
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
            }
            else if (transactionType == ShopType.Sell)
            {
                base.ChooseCharacter(character, initializeCursor);
            }
        }

        protected override void SoftChooseCharacter(CombatParticipant character)
        {
            if (transactionType == ShopType.Buy)
            {
                UpdateKnapsackView(character);
            }
            else if (transactionType == ShopType.Sell)
            {
                base.SoftChooseCharacter(character);
            }
        }
        #endregion

        #region SellSpecificOverrides
        protected override List<ChoiceActionPair> GetChoiceActionPairs(int inventorySlot)
        {
            if (transactionType == ShopType.Sell)
            {
                List<ChoiceActionPair> choiceActionPairs = new List<ChoiceActionPair>();
                if (selectedKnapsack == null) { return choiceActionPairs; }
                InventoryItem inventoryItem = selectedKnapsack.GetItemInSlot(inventorySlot);
                if (inventoryItem == null) { return choiceActionPairs; }

                // Sell
                if (selectedCharacter.TryGetComponent(out Knapsack selectedCharacterKnapsack))
                {
                    ChoiceActionPair sellActionPair = new ChoiceActionPair(messageOptionSell, () => shopper.CompleteTransaction(ShopType.Sell, inventoryItem, selectedCharacterKnapsack));
                    choiceActionPairs.Add(sellActionPair);
                }

                // Cancel
                ChoiceActionPair cancelActionPair = new ChoiceActionPair(messageOptionCancelSale, () => { });
                choiceActionPairs.Add(cancelActionPair);

                return choiceActionPairs;
            }
            else if (transactionType == ShopType.Buy)
            {
                return base.GetChoiceActionPairs(inventorySlot);
            }
            return null;
        }

        protected override void ChooseItem(int inventorySlot)
        {
            if (transactionType == ShopType.Sell)
            {
                // Check if item is sellable
                InventoryItem inventoryItem = selectedKnapsack.GetItemInSlot(inventorySlot);
                if (inventoryItem == null) { return; }

                if (inventoryItem.GetType() == typeof(KeyItem))
                {
                    SpawnMessage(messageCannotSell);
                }
                else
                {
                    SpawnSellMenu(inventorySlot);
                }
            }
            else if (transactionType == ShopType.Buy)
            {
                base.ChooseItem(inventorySlot);
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