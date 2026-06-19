using System;

namespace Frankie.Inventory
{
    [Serializable]
    public class WalletSaveData
    {
        public int cash;
        public int pendingCash;

        public WalletSaveData(int cash, int pendingCash)
        {
            this.cash = cash;
            this.pendingCash = pendingCash;
        }
    }
}
