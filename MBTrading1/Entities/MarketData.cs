
namespace MBTrading
{
    public class MarketData
    {
        public int DataType;
        public double Value;
        public double Price;
        public MarketTime Time;

        public MarketData(int mDataType, double dValue, double dPrice, MarketTime mTime)
        {
            this.DataType = mDataType;
            this.Value = dValue;
            this.Price = dPrice;
            this.Time = mTime;
        }
    }

    public static class MarketDataType
    {
        public const int Symbol   = 858796081;
        public const int Price    = 842018866;
        public const int Bid      = 858796082;
        public const int Ask      = 875573298;
        public const int BidSize  = 892350514;
        public const int AskSize  = 909127730;
        public const int LastSize = 925904946;
        public const int Volume   = 842084402;
        public const int Time     = 875638834;
    }
}
