using Frankie.Saving;
using Frankie.Utils;
using System;
using UnityEngine;

namespace Frankie.Inventory
{
    public class Wallet : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] int initialCash = 50;
        [SerializeField] int maxCash = 999999999;

        // State
        LazyValue<int> cash;
        int pendingCash = 0;

        // Events
        public event Action walletUpdated;

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
        public bool HasFunds(int value)
        {
            return cash.value >= value;
        }
        
        public bool IsWalletFull()
        {
            return cash.value >= maxCash;
        }

        public bool IsWalletEmpty()
        {
            return cash.value <= 0;
        }

        public int GetCash()
        {
            return cash.value;
        }

        public int GetPendingCash()
        {
            return pendingCash;
        }

        public void UpdateCash(int value)
        {
            cash.value = Mathf.Clamp(cash.value + value, 0, maxCash);

            walletUpdated?.Invoke();
        }

        public void UpdatePendingCash(int value)
        {
            pendingCash += value;
        }

        public bool TransferToWallet(int value)
        {
            if (value == 0) { return false; }

            // Bank -> Wallet
            if (value > 0)
            {
                if (cash.value + value > maxCash) { value = maxCash - (cash.value + value); } // Set to delta(max, addition)
                if (value > pendingCash) { value = pendingCash; } // Set to max transferrable from bank if over
            }

            // Wallet -> Bank
            if (value < 0 && -value > cash.value) { value = cash.value; } // Set to max transferrable form wallet if over

            cash.value += value;
            pendingCash -= value;

            walletUpdated?.Invoke();
            return true;
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
            WalletSaveData walletSaveData = state.GetState(typeof(WalletSaveData)) as WalletSaveData;
            if (walletSaveData != null)
            {
                cash.value = walletSaveData.cash;
                pendingCash = walletSaveData.pendingCash;
            }
        }
        #endregion
    }
}