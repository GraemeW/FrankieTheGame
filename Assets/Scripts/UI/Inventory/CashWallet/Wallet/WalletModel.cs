using System;
using Unity.Properties;
using LowDefMustard.UIBox;

namespace Frankie.Inventory.UI
{
    public sealed class WalletModel : BindableModel, IDisposable
    {
        // State
        private int internalCash;

        // Cached References
        private readonly Wallet wallet;

        [CreateProperty] public int cash
        {
            get => internalCash;
            private set => SetProperty(ref internalCash, value);
        }

        // Constructor
        public WalletModel(Wallet wallet)
        {
            this.wallet = wallet;
            internalCash = wallet.GetCash();
            wallet.walletUpdated += HandleWalletUpdated;
        }

        public void Dispose()
        {
            if (wallet != null) { wallet.walletUpdated -= HandleWalletUpdated; }
        }

        private void HandleWalletUpdated() => cash = wallet.GetCash();
    }
}
