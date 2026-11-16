namespace Frankie.Inventory
{
    public class TradeData
    {
        public TradeDataType tradeDataType { get; private set; }
        public ShopType shopType { get; private set; }
        public BankType bankType { get; private set; }

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
