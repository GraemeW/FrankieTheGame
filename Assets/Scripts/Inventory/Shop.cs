using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Frankie.Control;
using Frankie.Core.GameStateModifiers;
using Frankie.Utils.Localization;

namespace Frankie.Inventory
{
    [ExecuteInEditMode]
    public class Shop : MonoBehaviour, IGameStateModifierHandler, ILocalizable
    {
        // Interface Properties
        [SerializeField] private string backingHandlerGUID;
        public string handlerGUID { get => backingHandlerGUID; set => backingHandlerGUID = value; }
        
        [SerializeField][HideInInspector] private int backingListHashCheck;
        public int modifierListHashCheck { get => backingListHashCheck; set => backingListHashCheck = value; }
        
        [SerializeField][HideInInspector] private bool backingHasGameStateModifiers;
        public bool hasGameStateModifiers { get => backingHasGameStateModifiers; set => backingHasGameStateModifiers = value; }
        
        [SerializeField] [HideInInspector] private List<string> backingGameStateModifierGUIDs;
        public List<string> gameStateModifierGUIDs { get => backingGameStateModifierGUIDs; set => backingGameStateModifierGUIDs = value ?? new List<string>(); } 
        
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
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Inventory, true)] private LocalizedString localizedMessageForSale;
        [SerializeField][SimpleLocalizedString(LocalizationTableType.Inventory, true)] private LocalizedString localizedMessageCannotSell;
        [Header("Transaction Events")]
        [SerializeField] private UnityEvent transactionCompleted;

        // State
        private PlayerStateMachine playerStateMachine;
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
        #endregion
        
        #region UnityMethods
        private void OnDestroy()
        {
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
            setPlayerStateMachine.EnterShop(this);

            // Stash for listening to events
            playerStateMachine = setPlayerStateMachine;
            shopper = setPlayerStateMachine.GetComponent<Shopper>();

            // Set up events
            playerStateMachine.playerStateChanged += HandlePlayerState; // Exists to unsubscribe shopper
            shopper.transactionCompleted += HandleTransaction;
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
        private void HandlePlayerState(PlayerStateType playerState, IPlayerStateContext playerStateContext)
        {
            // Any state change implies exit shop, unsubscribe
            playerStateMachine.playerStateChanged -= HandlePlayerState;
            shopper.transactionCompleted -= HandleTransaction;

            playerStateMachine = null;
            shopper = null;
        }

        private void HandleTransaction()
        {
            transactionCompleted.Invoke();
        }
        #endregion
    }
}
