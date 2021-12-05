using Frankie.Saving;
using Frankie.Utils;
using UnityEngine;

namespace Frankie.Inventory
{
    public class Wallet : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] int initialCash = 50;

        // State
        LazyValue<int> cash;
        int pendingCash = 0;

        #region UnityMethods
        private void Awake()
        {
            cash = new LazyValue<int>(GetInitialCash);
        }

        private void Start()
        {
            cash.ForceInit();
        }

        private int GetInitialCash()
        {
            return initialCash;
        }
        #endregion

        #region PublicMethods
        public void UpdateCash(int value)
        {
            cash.value += value;
        }

        public int GetCash()
        {
            return cash.value;
        }

        public bool HasFunds(int value)
        {
            return cash.value >= value;
        }

        public void UpdatePendingCash(int value)
        {
            pendingCash += value;
        }

        public void TransferToWallet(int value)
        {
            if (value == 0) { return; }
            if (value > 0 && value > pendingCash) { value = pendingCash; } // Bank -> Wallet
            if (value < 0 && -value > cash.value) { value = cash.value; } // Wallet -> Bank

            cash.value += value;
            pendingCash -= value;
        }
        #endregion

        #region Interfaces
        [System.Serializable]
        private class WalletSaveData
        {
            public int cash;
            public int pendingCash;
        }

        public LoadPriority GetLoadPriority()
        {
            return LoadPriority.ObjectProperty;
        }

        public SaveState CaptureState()
        {
            WalletSaveData walletSaveData = new WalletSaveData
            {
                cash = cash.value,
                pendingCash = pendingCash
            };
            SaveState saveState = new SaveState(GetLoadPriority(), walletSaveData);
            return saveState;
        }

        public void RestoreState(SaveState state)
        {
            WalletSaveData walletSaveData = state.GetState() as WalletSaveData;
            if (walletSaveData != null)
            {
                cash.value = walletSaveData.cash;
                pendingCash = walletSaveData.pendingCash;
            }
        }
        #endregion
    }
}