using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Core;
using Frankie.Control;
using Frankie.Combat;
using Frankie.Stats;
using Frankie.Utils;
using Frankie.Utils.Localization;
using Frankie.Speech.UI;

namespace Frankie.Inventory.UI
{
    public class InventoryShopBox : InventoryBox
    {
        // Tunables
        [Header("Inventory-Shop Messages")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionSell;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionCancelSale;
        [Header("Inventory-Shop Prefabs")]
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

        #region LocalizationMethods
        public override List<TableEntryReference> GetLocalizationEntries()
        {
            // Note:  Standard configuration re-uses localization keys from InventoryBox 
            // Here we only return unique to this child script to prevent deletion of InventoryBox keys
            // Overridden standard Inventory entries would need to be manually deleted
            return new List<TableEntryReference>
            {
                localizedOptionSell.TableEntryReference,
                localizedOptionCancelSale.TableEntryReference,
            };
        }
        #endregion
        
        #region Initialization
        // Buy-specific
        public void Setup(BaseController baseController, PartyCombatConduit partyCombatConduit, Shopper setShopper, ShopBox setShopBox, InventoryItem setBuyItem, string setMessageNoSpace)
        {
            transactionType = ShopType.Buy;
            
            base.Setup(baseController, partyCombatConduit, null, false);
            shopper = setShopper;
            shopBox = setShopBox;
            buyItem = setBuyItem;
            messageNoSpace = setMessageNoSpace;
        }

        // Sell-specific
        public void Setup(BaseController baseController, PlayerStateMachine setPlayerStateMachine, PartyCombatConduit partyCombatConduit, Shopper setShopper, string setMessageForSale, string setMessageCannotSell)
        {
            transactionType = ShopType.Sell;

            base.Setup(baseController, partyCombatConduit, null, false);
            playerStateMachine = setPlayerStateMachine;
            shopper = setShopper;
            messageForSale = setMessageForSale;
            messageCannotSell = setMessageCannotSell;

            SetupWalletUI();
            baseController.AddInputReceiver(this, null);
        }

        private void SetupWalletUI()
        {
            walletUI = Instantiate(walletUIPrefab, transform.parent);
        }

        protected override void OnDestroy()
        {
            if (shopBox != null) { shopBox.UpdateShopMessageToSuccess(); }
            if (walletUI != null) { Destroy(walletUI.gameObject); }

            base.OnDestroy();
            if (transactionType == ShopType.Sell && playerStateMachine != null) { playerStateMachine.EnterWorld(); }
        }
        #endregion

        #region BuySpecificOverrides
        protected override void ChooseCharacter(CombatParticipant character, bool initializeCursor = true)
        {
            switch (transactionType)
            {
                case ShopType.Buy:
                    TryBuyForCharacter(character);
                    break;
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

        private void TryBuyForCharacter(CombatParticipant character)
        {
            UpdateKnapsackView(character);
            SetInventoryBoxState(InventoryBoxState.InCharacterSelection);

            var characterKnapsack = selectedCharacter.GetComponent<Knapsack>();
            var selectedCharacterKnapsack = selectedCharacter.GetComponent<Knapsack>();
            if (characterKnapsack == null || selectedCharacterKnapsack == null) { return; }

            if (selectedCharacterKnapsack.HasFreeSpace())
            {
                shopper.CompleteTransaction(ShopType.Buy, buyItem, characterKnapsack);
                Destroy(gameObject);
            }
            else
            {
                DialogueBox dialogueBox = SpawnDialogueBox(messageNoSpace, null);
                controller.AddInputReceiver(dialogueBox, () => SetInventoryBoxState(InventoryBoxState.InCharacterSelection));
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
                    var choiceActionPairs = new List<ChoiceActionPair>();
                    if (selectedKnapsack == null) { return choiceActionPairs; }
                    InventoryItem inventoryItem = selectedKnapsack.GetItemInSlot(inventorySlot);
                    if (inventoryItem == null) { return choiceActionPairs; }
                    
                    // Sale
                    if (selectedCharacter.TryGetComponent(out Knapsack selectedCharacterKnapsack))
                    {
                        var sellActionPair = new ChoiceActionPair(localizedOptionSell.GetSafeLocalizedString(), () => shopper.CompleteTransaction(ShopType.Sell, inventoryItem, selectedCharacterKnapsack));
                        choiceActionPairs.Add(sellActionPair);
                    }
                    
                    // Cancel
                    var cancelActionPair = new ChoiceActionPair(localizedOptionCancelSale.GetSafeLocalizedString(), () => { });
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
                    TrySellItem(inventorySlot);
                    break;
                case ShopType.Buy:
                    base.ChooseItem(inventorySlot);
                    break;
            }
        }

        private void TrySellItem(int inventorySlot)
        {
            // Check if item is sellable
            InventoryItem inventoryItem = selectedKnapsack.GetItemInSlot(inventorySlot);
            if (inventoryItem == null) { return; }

            if (inventoryItem.GetType() == typeof(KeyItem))
            {
                DialogueBox dialogueBox = SpawnDialogueBox(messageCannotSell);
                controller.AddInputReceiver(dialogueBox, ResetSelectState);
            }
            else
            {
                SpawnSellMenu(inventorySlot);
            }
        }

        private void SpawnSellMenu(int inventorySlot)
        {
            InventoryItem inventoryItem = selectedKnapsack.GetItemInSlot(inventorySlot);
            Shop shop = shopper.GetCurrentShop();
            if (inventoryItem == null || shop == null) { return; }

            int salePrice = Mathf.RoundToInt(inventoryItem.GetPrice() * shop.GetSaleDiscount());
            string saleMessage = string.Format(messageForSale, salePrice.ToString(CultureInfo.InvariantCulture));

            List<ChoiceActionPair> choiceActionPairs = GetChoiceActionPairs(inventorySlot);
            if (choiceActionPairs == null || choiceActionPairs.Count == 0) { return; }

            DialogueBox dialogueBox = SpawnDialogueBox(saleMessage, choiceActionPairs);
            controller.AddInputReceiver(dialogueBox, ResetSelectState);
        }
        #endregion
    }
}
