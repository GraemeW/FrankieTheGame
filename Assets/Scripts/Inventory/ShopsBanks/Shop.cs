using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using LowDefMustard.GameStateModifiers;
using LowDefMustard.Localization;
using Frankie.Core;
using Frankie.Utils.Localization;

namespace Frankie.Inventory
{
    [ExecuteInEditMode]
    public class Shop : MonoBehaviour, IGameStateModifierHandler, ILocalizable
    {
        // Note:  Internal methods/properties are for test visibility
        
        // Tunables
        [Header("Base Attributes")]
        [SerializeField] private List<InventoryItem> stock = new();
        [SerializeField] private ShopType shopType = ShopType.Both;
        [SerializeField] private float saleDiscount = 0.7f;
        [Header("Interaction Texts")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Inventory, true)] private LocalizedString localizedMessageIntro;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Inventory, true)] private LocalizedString localizedMessageSuccess;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Inventory, true)] private LocalizedString localizedMessageNoFunds;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Inventory, true)] private LocalizedString localizedMessageNoSpace;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Inventory, true)] private LocalizedString localizedMessageCannotSell;
        [Header("Interaction Texts = {0}: item name, {1}: coins")]
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Inventory, true)] private LocalizedString localizedMessageForSale;
        [Header("Transaction Events")]
        [SerializeField] private UnityEvent transactionCompleted;

        // Editor Field References
        internal const string stockField = nameof(stock);

        // GameState Modifier Properties
        [SerializeField][HideInInspector] private string backingHandlerGUID;
        public string handlerGUID { get => backingHandlerGUID; set => backingHandlerGUID = value; }
        [SerializeField][HideInInspector] private int backingListHashCheck;
        public int modifierListHashCheck { get => backingListHashCheck; set => backingListHashCheck = value; }
        [SerializeField][HideInInspector] private bool backingHasGameStateModifiers;
        public bool hasGameStateModifiers { get => backingHasGameStateModifiers; set => backingHasGameStateModifiers = value; }
        [SerializeField] [HideInInspector] private List<string> backingGameStateModifierGUIDs;
        public List<string> gameStateModifierGUIDs { get => backingGameStateModifierGUIDs; set => backingGameStateModifierGUIDs = value ?? new List<string>(); } 
        
        // State
        private Shopper shopper;

        #region PublicGetters
        public string GetMessageIntro() => localizedMessageIntro.GetSafeLocalizedString();
        public string GetMessageSuccess() => localizedMessageSuccess.GetSafeLocalizedString();
        public string GetMessageNoFunds() => localizedMessageNoFunds.GetSafeLocalizedString();
        public string GetMessageNoSpace() => localizedMessageNoSpace.GetSafeLocalizedString();
        public string GetMessageForSale() => localizedMessageForSale.GetSafeLocalizedString();
        public string GetMessageCannotSell() => localizedMessageCannotSell.GetSafeLocalizedString();
        public bool HasInventory() => stock.Count > 0;
        public IList<InventoryItem> GetShopStock() => stock;
        public ShopType GetShopType() => shopType;
        public float GetSaleDiscount() => saleDiscount;
        
        internal UnityEvent GetTransactionCompleted() => transactionCompleted ??= new UnityEvent();
        #endregion
        
        #region UnityMethods
        private void OnDestroy()
        {
            if (shopper != null) { shopper.transactionCompleted -= HandleTransaction; }
            ILocalizable.TriggerOnDestroy(this);
            IGameStateModifierHandler.TriggerOnDestroy(this);
        }
        
        private void OnDrawGizmos()
        {
            IGameStateModifierHandler.TriggerOnGizmos(this);
        }
        #endregion
        
        #region PublicUtility
        public void InitiateBargain(PlayerStateMachine setPlayerStateMachine) // Called via Unity events
        {
            // Note:  Stays subscribed until destroyed, and relays only while this is the shopper's current shop
            // A state-based unsubscribe would drop the relay for shops opened from a dialogue choice
            if (setPlayerStateMachine.TryGetComponent(out Shopper setShopper))
            {
                if (shopper != null && shopper != setShopper) { shopper.transactionCompleted -= HandleTransaction; }
                
                shopper = setShopper;
                shopper.transactionCompleted -= HandleTransaction;
                shopper.transactionCompleted += HandleTransaction;
            }
            setPlayerStateMachine.EnterShop(this);
        }
        #endregion
        
        #region InterfaceMethods
        public LocalizationTableType localizationTableType { get; } =  LocalizationTableType.Inventory;
        public List<TableEntryReference> GetLocalizationEntries()
        {
            return new List<TableEntryReference>
            {
                localizedMessageIntro.TableEntryReference,
                localizedMessageSuccess.TableEntryReference,
                localizedMessageNoFunds.TableEntryReference,
                localizedMessageNoSpace.TableEntryReference,
                localizedMessageForSale.TableEntryReference,
                localizedMessageCannotSell.TableEntryReference,
            };
        }

        public IList<GameStateModifier> GetGameStateModifiers()
        {
            List<GameStateModifier> gameStateModifiers = new();
            foreach (InventoryItem item in stock)
            {
                KeyItem keyItem = item as KeyItem;
                if (keyItem == null) { continue; }
                gameStateModifiers.Add(keyItem);
            }
            return gameStateModifiers;
        }
        #endregion
        
        #region PrivateMethods
        private void HandleTransaction()
        {
            if (shopper == null || shopper.GetCurrentShop() != this) { return; }
            transactionCompleted.Invoke();
        }
        #endregion
    }
}
