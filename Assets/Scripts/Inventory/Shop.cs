using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Frankie.Control;

namespace Frankie.Inventory
{
    public class Shop : MonoBehaviour
    {
        // Tunables
        [Header("Base Attributes")]
        [SerializeField] private InventoryItem[] stock;
        [SerializeField] private ShopType shopType = ShopType.Both;
        [SerializeField] private float saleDiscount = 0.7f;
        [Header("Interaction Texts")]
        [SerializeField] private string messageIntro = "What'cha want to buy?";
        [SerializeField] private string messageSuccess = "Thanks, want anything else?";
        [SerializeField] private string messageNoFunds = "Whoops, looks like you don't have enough cash for that";
        [SerializeField] private string messageNoSpace = "Whoops, looks like you don't have enough space for that";
        [Tooltip("{0} for sale amount")][SerializeField] private string messageForSale = "I can give you {0} for that";
        [SerializeField] private string messageCannotSell = "I can't accept that item";
        [Header("Transaction Events")]
        [SerializeField] UnityEvent transactionCompleted;

        // State
        private PlayerStateMachine playerStateMachine;
        private Shopper shopper;

        public void InitiateBargain(PlayerStateMachine setPlayerStateMachine) // Called via Unity events
        {
            if (stock == null) { return; }
            setPlayerStateMachine.EnterShop(this);

            // Stash for listening to events
            playerStateMachine = setPlayerStateMachine;
            shopper = setPlayerStateMachine.GetComponent<Shopper>();

            // Set up events
            playerStateMachine.playerStateChanged += HandlePlayerState; // Exists to unsubscribe shopper
            shopper.transactionCompleted += HandleTransaction;
        }

        public string GetMessageIntro() => messageIntro;
        public string GetMessageSuccess() => messageSuccess;
        public string GetMessageNoFunds() => messageNoFunds;
        public string GetMessageNoSpace() => messageNoSpace;
        public string GetMessageForSale() => messageForSale;
        public string GetMessageCannotSell() => messageCannotSell;

        public bool HasInventory() => stock.Length > 0;
        public IEnumerable<InventoryItem> GetShopStock() => stock;
        public ShopType GetShopType() => shopType;
        public float GetSaleDiscount() => saleDiscount;

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
