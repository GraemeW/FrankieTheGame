using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frankie.Inventory
{
    public class TradeData
    {
        public TradeDataType tradeDataType { get; } = default;
        public ShopType shopType { get; } = default;
        public BankType bankType { get; } = default;

        public TradeData(ShopType shopType)
        {
            tradeDataType = TradeDataType.Shop;
            this.shopType = shopType;
        }

        public TradeData(BankType bankType)
        {
            tradeDataType = TradeDataType.Bank;
            this.bankType = bankType;
        }

    }
}