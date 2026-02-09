using System;
using UnityEngine;
using Frankie.Saving;
using Frankie.Utils;

namespace Frankie.Inventory
{
    public class Wallet : MonoBehaviour, ISaveable
    {
        // Tunables
        [SerializeField] private int initialCash = 50;
        [SerializeField] private int maxCash = 999999999;

        // State
        private LazyValue<int> cash;
        private int pendingCash = 0;

        // Events
        public event Action walletUpdated;

        #region UnityMethods
        private void Awake()
        {
            cash = new LazyValue<int>(GetInitialCash);
        }
        private int GetInitialCash() => initialCash;

        private void Start()
        {
            cash.ForceInit();
        }
        #endregion

        #region PublicMethods
        public bool HasFunds(int value) => cash.value >= value;
        public bool IsWalletFull() => cash.value >= maxCash;
        public bool IsWalletEmpty() => cash.value <= 0;
        public int GetCash() => cash.value;
        public int GetPendingCash() => pendingCash;

        public void UpdateCash(int value)
        {
            cash.value = Mathf.Clamp(cash.value + value, 0, maxCash);
            walletUpdated?.Invoke();
        }

        public void UpdatePendingCash(int value)
        {
            pendingCash += value;
        }

        public void TransferToWallet(int value)
        {
            if (value == 0) { return; }

            // Bank -> Wallet
            if (value > 0)
            {
                if (cash.value + value > maxCash) { value = maxCash - (cash.value + value); } // Set to delta(max, addition)
                if (value > pendingCash) { value = pendingCash; } // Set to max transferable from bank if over
            }

            // Wallet -> Bank
            if (value < 0 && -value > cash.value) { value = cash.value; } // Set to max transferable form wallet if over

            cash.value += value;
            pendingCash -= value;

            walletUpdated?.Invoke();
        }
        #endregion

        #region Interfaces
        [Serializable]
        private class WalletSaveData
        {
            public int cash;
            public int pendingCash;
        }

        public bool IsCorePlayerState() => true;
        
        public LoadPriority GetLoadPriority() => LoadPriority.ObjectProperty;

        public SaveState CaptureState()
        {
            var walletSaveData = new WalletSaveData
            {
                cash = cash.value,
                pendingCash = pendingCash
            };
            return new SaveState(GetLoadPriority(), walletSaveData);
        }

        public void RestoreState(SaveState state)
        {
            if (state.GetState(typeof(WalletSaveData)) is not WalletSaveData walletSaveData) { return; }
            cash.value = walletSaveData.cash;
            pendingCash = walletSaveData.pendingCash;
        }
        #endregion
    }
}
