using Frankie.Inventory;
using Frankie.Saving;
using Frankie.Stats;
using Frankie.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Control.Specialization
{
    public class WorldCashGiverTaker : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] int transactionCash = 10;
        [SerializeField] bool infiniteTransactions = false;
        [SerializeField] [Tooltip("Ignored if infiniteTransactions set to true")] [Min(1)] int numberTransactions = 1;
        [SerializeField] [Tooltip("{0} for character name, {1} for cash quantity")] string messageTransactionPositive = "Wow!  Looks like {0} found ${1}.";
        [SerializeField] [Tooltip("{0} for character name, {1} for cash quantity")] string messageTransactionNegative = "Yikes!  {0} just lost ${1}.";
        [SerializeField] [Tooltip("{0} for character name")] string messageWalletFull = "Doesn't look like {0} can hold any more cash.";
        [SerializeField] [Tooltip("{0} for character name")] string messageWalletEmpty = "{0} is already broke, can't get more broke.";
        [SerializeField] bool announceNothing = true;
        [SerializeField] string messageNothing = "Oh, looks like it's NOTHING.";

        // State
        LazyValue<int> numberTransactionsLeft;

        private void Awake()
        {
            numberTransactionsLeft = new LazyValue<int>(GetInitialTransactionCount);
        }

        private void Start()
        {
            numberTransactionsLeft.ForceInit();
        }

        private int GetInitialTransactionCount()
        {
            return numberTransactions;
        }

        public void ConductTransaction(PlayerStateMachine playerStateHandler) // Called by Unity Events
        {
            if (IsNothingLeft(playerStateHandler)) { return; }

            Wallet wallet = playerStateHandler.GetComponent<Wallet>();
            Party party = playerStateHandler.GetComponent<Party>();
            string partyLeaderName = party.GetPartyLeaderName();

            if (IsWalletFullOrEmpty(playerStateHandler, wallet, partyLeaderName)) { return; }

            if (transactionCash > 0)
            {
                playerStateHandler.EnterDialogue(string.Format(messageTransactionPositive, partyLeaderName, transactionCash.ToString()));
            }
            else if (transactionCash < 0)
            {
                playerStateHandler.EnterDialogue(string.Format(messageTransactionNegative, partyLeaderName, Mathf.Abs(transactionCash).ToString()));
            }
            wallet.UpdateCash(transactionCash);
            if (!infiniteTransactions) { numberTransactionsLeft.value--; }
        }

        private bool IsNothingLeft(PlayerStateMachine playerStateHandler)
        {
            if (transactionCash == 0) { return true; }
            if (!infiniteTransactions && numberTransactionsLeft.value <= 0)
            {
                if (announceNothing) { playerStateHandler.EnterDialogue(messageNothing); }
                return true;
            }
            return false;
        }

        private bool IsWalletFullOrEmpty(PlayerStateMachine playerStateHandler, Wallet wallet, string recipient)
        {
            if (transactionCash > 0 && wallet.IsWalletFull())
            {
                playerStateHandler.EnterDialogue(string.Format(messageWalletFull, recipient));
                return true;
            }
            else if (transactionCash < 0 && wallet.IsWalletEmpty())
            {
                playerStateHandler.EnterDialogue(string.Format(messageWalletEmpty, recipient));
                return true;
            }
            return false;
        }

        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        public SaveState CaptureState()
        {
            SaveState saveState = new SaveState(GetLoadPriority(), numberTransactionsLeft.value);
            return saveState;
        }

        public void RestoreState(SaveState state)
        {
            numberTransactionsLeft.value = (int)state.GetState();
        }
    }
}