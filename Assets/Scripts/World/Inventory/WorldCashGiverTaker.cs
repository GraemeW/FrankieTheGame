using UnityEngine;
using Frankie.Control;
using Frankie.Saving;
using Frankie.Inventory;
using Frankie.Stats;
using Frankie.Utils;


namespace Frankie.World
{
    public class WorldCashGiverTaker : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] private int transactionCash = 10;
        [SerializeField] private bool infiniteTransactions = false;
        [SerializeField][Tooltip("Ignored if infiniteTransactions set to true")][Min(1)] private int numberTransactions = 1;
        [SerializeField][Tooltip("{0} for character name, {1} for cash quantity")] private string messageTransactionPositive = "Wow!  Looks like {0} found ${1}.";
        [SerializeField][Tooltip("{0} for character name, {1} for cash quantity")] private string messageTransactionNegative = "Yikes!  {0} just lost ${1}.";
        [SerializeField][Tooltip("{0} for character name")] private string messageWalletFull = "Doesn't look like {0} can hold any more cash.";
        [SerializeField][Tooltip("{0} for character name")] private string messageWalletEmpty = "{0} is already broke, can't get more broke.";
        [SerializeField] private bool announceNothing = true;
        [SerializeField] private string messageNothing = "Oh, looks like it's NOTHING.";
        [SerializeField] private InteractionEvent transactionSuccessful;

        // State
        private LazyValue<int> numberTransactionsLeft;

        #region UnityMethods
        private void Awake()
        {
            numberTransactionsLeft = new LazyValue<int>(GetInitialTransactionCount);
        }

        private void Start()
        {
            numberTransactionsLeft.ForceInit();
        }
        private int GetInitialTransactionCount() => numberTransactions;
        #endregion

        #region PublicMethods
        public void ConductTransaction(PlayerStateMachine playerStateHandler) // Called by Unity Events
        {
            if (IsNothingLeft(playerStateHandler)) { return; }

            var wallet = playerStateHandler.GetComponent<Wallet>();
            var party = playerStateHandler.GetComponent<Party>();
            string partyLeaderName = party.GetPartyLeaderName();

            if (IsWalletFullOrEmpty(playerStateHandler, wallet, partyLeaderName)) { return; }

            switch (transactionCash)
            {
                case > 0:
                    playerStateHandler.EnterDialogue(string.Format(messageTransactionPositive, partyLeaderName, transactionCash.ToString()));
                    break;
                case < 0:
                    playerStateHandler.EnterDialogue(string.Format(messageTransactionNegative, partyLeaderName, Mathf.Abs(transactionCash).ToString()));
                    break;
            }
            wallet.UpdateCash(transactionCash);
            if (!infiniteTransactions) { numberTransactionsLeft.value--; }
            transactionSuccessful?.Invoke(playerStateHandler);
        }
        #endregion

        #region PrivateMethods
        private bool IsNothingLeft(PlayerStateMachine playerStateHandler)
        {
            if (transactionCash == 0) { return true; }
            if (infiniteTransactions || numberTransactionsLeft.value > 0) { return false; }
            
            if (announceNothing) { playerStateHandler.EnterDialogue(messageNothing); }
            return true;
        }

        private bool IsWalletFullOrEmpty(PlayerStateMachine playerStateHandler, Wallet wallet, string recipient)
        {
            switch (transactionCash)
            {
                case > 0 when wallet.IsWalletFull():
                    playerStateHandler.EnterDialogue(string.Format(messageWalletFull, recipient));
                    return true;
                case < 0 when wallet.IsWalletEmpty():
                    playerStateHandler.EnterDialogue(string.Format(messageWalletEmpty, recipient));
                    return true;
                default:
                    return false;
            }
        }
        #endregion

        #region SaveImplementation
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty; 

        public SaveState CaptureState()
        {
            var saveState = new SaveState(GetLoadPriority(), numberTransactionsLeft.value);
            return saveState;
        }

        public void RestoreState(SaveState state)
        {
            numberTransactionsLeft.value = (int)state.GetState(typeof(int));
        }
        #endregion
    }
}
