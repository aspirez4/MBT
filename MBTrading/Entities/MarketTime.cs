
namespace MBTrading
{
    public class MarketTime
    {
        public int Hour;
        public int MinutePrimary;
        public int MinuteSecondary;
        public int OigMinute;

        // Ctor
        public MarketTime(int nHour, int nMinute)
        {
            this.Hour = (int)((int)((nHour * 60) / (Consts.MINUTE_CANDLES_PRIMARY)) * (Consts.MINUTE_CANDLES_PRIMARY / 60.0));
            this.MinutePrimary = (nMinute / Consts.MINUTE_CANDLES_PRIMARY) * Consts.MINUTE_CANDLES_PRIMARY;
            this.MinuteSecondary = (nMinute / Consts.MINUTE_CANDLES_SECONDARY) * Consts.MINUTE_CANDLES_SECONDARY;
            this.OigMinute = nMinute;
        }
        
        public override string ToString()
        {
            return (string.Format("{0}:{1}", this.Hour.ToString("D2"), this.MinuteSecondary.ToString("D2")));
        }
 
        public int Delta(MarketTime mt1)
        {
            int nMinutes = 0;

            if (this.Hour >= mt1.Hour)
            {
                nMinutes = ((this.Hour - mt1.Hour) * 60) + (this.OigMinute - mt1.OigMinute);
            }
            else
            {
                nMinutes = ((23 - this.Hour + mt1.Hour) * 60) + (60 - this.OigMinute + mt1.OigMinute);
            }

            return (nMinutes);
        }

        // Operator overloading
        public bool Compare(MarketTime m2, bool bPrimary)
        {
            if (bPrimary)
            {
                return ((this.MinutePrimary == m2.MinutePrimary) && (this.Hour == m2.Hour));
            }
            else
            {
                return ((this.MinuteSecondary == m2.MinuteSecondary) && (this.Hour == m2.Hour));
            }
        }
    }
}
