using System;
using Frankie.Inventory;

namespace Frankie.Core.PlayerStateMemory
{
    public class TradeMemory
    {
        public TradeData tradeData;

        public bool InitiateTrade(Action instantiateShop, Action instantiateBank)
        {
            if (tradeData == null) { return false; }
            switch (tradeData.tradeDataType)
            {
                case TradeDataType.Shop:
                    if (instantiateShop == null) { return false; }
                    instantiateShop.Invoke();
                    break;
                case TradeDataType.Bank:
                    if (instantiateBank == null) { return false; }
                    instantiateBank.Invoke();
                    break;
                case TradeDataType.None:
                    return false;
            }
            return true;
        }
    }
}
