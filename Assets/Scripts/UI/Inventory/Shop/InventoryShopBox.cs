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
using Frankie.Speech.UI;
using Frankie.Utils.UI;
using Frankie.Utils.Localization;

namespace Frankie.Inventory.UI
{
    public class InventoryShopBox : InventoryBox
    {
        // Tunables
        [Header("Inventory-Shop Messages")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionSell;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedOptionCancelSale;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.UI, true)] private LocalizedString localizedMessageEquipItem;
        [Header("Inventory-Shop Hookups")]
        [SerializeField] private Transform statSheetParent;
        [Header("Inventory-Shop Prefabs")]
        [SerializeField] private WalletUI walletUIPrefab;
        [SerializeField] private StatChangeField statChangeFieldPrefab;

        // State
        private ShopType transactionType = ShopType.Both;
        private InventoryItem buyItem;
        
        // UI State
        private DialogueBox equipConfirmBox;
        private DialogueBox equipSellBox;

        // Cached References
        private PlayerStateMachine playerStateMachine;
        private WalletUI walletUI;
        private ShopBox shopBox;
        private Shopper shopper;
        private Shop shop;

        // UIBox Configuration
        protected override EnumLookup<InventoryBoxState,UIBoxStateBehaviour> BuildStateBehaviours()
        {
            var equipmentInventoryConfiguration = base.BuildStateBehaviours();
            foreach (UIBoxStateBehaviour stateBehaviour in equipmentInventoryConfiguration.GetValues<InventoryBoxState>())
            {
                stateBehaviour.reconcileChoiceOptions = ImplementReconcileChoiceOptions;
            }
            return equipmentInventoryConfiguration;
        }
        
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
        public void Setup(BaseController baseController, PartyCombatConduit partyCombatConduit, Shopper setShopper, Shop setShop, ShopBox setShopBox, InventoryItem setBuyItem)
        {
            transactionType = ShopType.Buy;
            
            base.Setup(baseController, partyCombatConduit, null, false);
            shopper = setShopper;
            shop = setShop;
            shopBox = setShopBox;
            buyItem = setBuyItem;
            
            RefreshKnapsackContents();
        }

        private void ImplementReconcileChoiceOptions()
        {
            if (transactionType == ShopType.Buy) { SetChoiceAvailable(true); }
            else { StandardReconcileChoiceOptions(); }
        }

        // Sell-specific
        public void Setup(BaseController baseController, PlayerStateMachine setPlayerStateMachine, PartyCombatConduit partyCombatConduit, Shopper setShopper, Shop setShop)
        {
            transactionType = ShopType.Sell;

            base.Setup(baseController, partyCombatConduit, null, false);
            playerStateMachine = setPlayerStateMachine;
            shopper = setShopper;
            shop = setShop;

            SetupWalletUI();
            baseController.AddInputReceiver(this, null);
        }

        protected override void PopulateKnapsackContents()
        {
            CleanOldStatSheet();
            switch (transactionType)
            {
                case ShopType.Buy:
                    if (buyItem is EquipableItemBase equipableItem && CanEquipItem(equipableItem, out Equipment equipment)) { PopulateStatComparisonPanel(equipableItem, equipment); }
                    else { base.PopulateKnapsackContents(); }
                    break;
                case ShopType.Sell:
                    base.PopulateKnapsackContents();
                    break;
            }
        }

        private void SetupWalletUI()
        {
            walletUI = Instantiate(walletUIPrefab, transform.parent);
        }

        protected override void DestroyTriggered()
        {
            if (shopBox != null) { shopBox.UpdateShopMessageToSuccess(); }
            if (walletUI != null) { Destroy(walletUI.gameObject); }
            if (equipConfirmBox != null) { Destroy(equipConfirmBox.gameObject); }
            if (equipSellBox != null) { Destroy(equipSellBox.gameObject); }
            
            if (transactionType == ShopType.Sell && playerStateMachine != null) { playerStateMachine.EnterWorld(); }
        }
        #endregion

        #region BuySpecificOverrides
        protected override void ChooseCharacter(CombatParticipant character, bool initializeCursor = true, bool triggerUIBoxModified = true)
        {
            switch (transactionType)
            {
                case ShopType.Buy:
                    TryBuyForCharacter(character);
                    break;
                case ShopType.Sell:
                    base.ChooseCharacter(character, initializeCursor, triggerUIBoxModified);
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
            var characterKnapsack = selectedCharacter.GetComponent<Knapsack>();
            var selectedCharacterKnapsack = selectedCharacter.GetComponent<Knapsack>();
            if (characterKnapsack == null || selectedCharacterKnapsack == null) { return; }

            if (selectedCharacterKnapsack.HasFreeSpace())
            {
                shopper.CompleteTransaction(ShopType.Buy, buyItem, characterKnapsack);
                if (buyItem is EquipableItemBase equipableItem && CanEquipItem(equipableItem, out Equipment equipment))
                {
                    // Destruction handled via Equip Item OptionBox
                    equipConfirmBox = SpawnEquipItemOptionBox(equipableItem, equipment);
                } 
                else { destroyQueued = true; }
            }
            else
            {
                DialogueBox dialogueBox = SpawnDialogueBox(shop.GetMessageNoSpace(), null);
                controller.AddInputReceiver(dialogueBox, () => SetInventoryBoxState(InventoryBoxState.InCharacterSelection));
            }
        }
        #endregion
        
        #region EquipmentContext
        private bool CanEquipItem(EquipableItemBase equipableItem, out Equipment equipment)
        {
            equipment = null;
            if (selectedKnapsack == null) { return false; }
            return selectedCharacter.TryGetComponent(out equipment) && equipableItem.CanUseItem(equipment);
        }
        
        private void PopulateStatComparisonPanel(EquipableItemBase equipableItem, Equipment equipment)
        {
            if (selectedKnapsack == null) { return; }
            if (!selectedCharacter.TryGetComponent(out BaseStats baseStats)) { return; }
            
            foreach (StatComparison statComparison in Equipment.GetStatComparisons(baseStats, equipment, equipableItem, equipableItem.GetEquipLocation()))
            {
                StatChangeField statChangeField = Instantiate(statChangeFieldPrefab, statSheetParent);
                statChangeField.Setup(statComparison);
            }
        }

        private DialogueBox SpawnEquipItemOptionBox(EquipableItemBase equipableItem, Equipment equipment)
        {
            var choiceActionPairs = new List<ChoiceActionPair>();
            var confirmEquip = new ChoiceActionPair(localizedConfirmChoiceAffirmative.GetSafeLocalizedString(), () =>
            {
                EquipableItemBase oldEquipableItem = equipment.GetItemInSlot(equipableItem.GetEquipLocation());
                equipment.AddEquipment(equipableItem, true);

                transactionType = ShopType.Sell;
                equipSellBox = SpawnSellMenu(oldEquipableItem, true);
                if (equipSellBox == null) { destroyQueued = true; }
            });
            choiceActionPairs.Add(confirmEquip);
            var rejectEquip = new ChoiceActionPair(localizedConfirmChoiceNegative.GetSafeLocalizedString(), () => { destroyQueued = true; });
            choiceActionPairs.Add(rejectEquip);

            DialogueBox dialogueBox = SpawnDialogueBox(string.Format(localizedMessageEquipItem.GetSafeLocalizedString(), equipableItem.GetDisplayName()), choiceActionPairs);
            controller.AddInputReceiver(dialogueBox, () => destroyQueued = true);
            dialogueBox.ClearDisableCallbacksOnChoose(true);
            return dialogueBox;
        }
        
        private void CleanOldStatSheet()
        {
            foreach (Transform child in statSheetParent)
            {
                Destroy(child.gameObject);
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
                DialogueBox dialogueBox = SpawnDialogueBox(shop.GetMessageCannotSell());
                controller.AddInputReceiver(dialogueBox, ResetSelectState);
            }
            else
            {
                SpawnSellMenu(inventorySlot);
            }
        }

        private DialogueBox SpawnSellMenu(InventoryItem inventoryItem, bool destroyOnSale = false)
        {
            if (selectedKnapsack == null) { return null; }
            int inventoryItemSlot = selectedKnapsack.FindSlotWithItem(inventoryItem);
            return inventoryItemSlot >= 0 ? SpawnSellMenu(inventoryItemSlot, destroyOnSale) : null;
        }
        
        private DialogueBox SpawnSellMenu(int inventorySlot, bool destroyOnSale = false)
        {
            InventoryItem inventoryItem = selectedKnapsack.GetItemInSlot(inventorySlot);
            Shop shop = shopper.GetCurrentShop();
            if (inventoryItem == null || shop == null) { return null; }

            int salePrice = Mathf.RoundToInt(inventoryItem.GetPrice() * shop.GetSaleDiscount());
            string saleMessage = string.Format(shop.GetMessageForSale(), inventoryItem.GetDisplayName(), salePrice.ToString(CultureInfo.InvariantCulture));

            List<ChoiceActionPair> choiceActionPairs = GetChoiceActionPairs(inventorySlot);
            if (choiceActionPairs == null || choiceActionPairs.Count == 0) { return null; }

            DialogueBox dialogueBox = SpawnDialogueBox(saleMessage, choiceActionPairs);
            if (destroyOnSale) { controller.AddInputReceiver(dialogueBox, () => destroyQueued = true); }
            else { controller.AddInputReceiver(dialogueBox, ResetSelectState); }
            return dialogueBox;
        }
        #endregion
    }
}
