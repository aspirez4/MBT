
namespace MBTrading
{
    public class MarketTime
    {
        public int HourPrimery;
        public int HourSecondery;
        public int MinutePrimery;
        public int MinuteSecondary;
        public int OigMinute;

        // Ctor
        public MarketTime(int nHour, int nMinute)
        {
            this.HourPrimery = (int)((int)((nHour * 60) / (Consts.MINUTE_CANDLES_PRIMARY)) * (Consts.MINUTE_CANDLES_PRIMARY / 60.0));
            this.HourSecondery = (int)((int)((nHour * 60) / (Consts.MINUTE_CANDLES_SECONDARY)) * (Consts.MINUTE_CANDLES_SECONDARY / 60.0));
            this.MinutePrimery = (nMinute / Consts.MINUTE_CANDLES_PRIMARY) * Consts.MINUTE_CANDLES_PRIMARY;
            this.MinuteSecondary = (nMinute / Consts.MINUTE_CANDLES_SECONDARY) * Consts.MINUTE_CANDLES_SECONDARY;
            this.OigMinute = nMinute;
        }
        
        public override string ToString()
        {
            return (string.Format("{0}:{1}", this.HourPrimery.ToString("D2"), this.MinuteSecondary.ToString("D2")));
        }
 
        public int Delta(MarketTime mt1)
        {
            int nMinutes = 0;

            if (this.HourPrimery >= mt1.HourPrimery)
            {
                nMinutes = ((this.HourPrimery - mt1.HourPrimery) * 60) + (this.OigMinute - mt1.OigMinute);
            }
            else
            {
                nMinutes = ((23 - this.HourPrimery + mt1.HourPrimery) * 60) + (60 - this.OigMinute + mt1.OigMinute);
            }

            return (nMinutes);
        }

        // Operator overloading
        public bool Compare(MarketTime m2, bool bPrimary)
        {
            if (bPrimary)
            {
                return ((this.MinutePrimery == m2.MinutePrimery) && (this.HourPrimery == m2.HourPrimery));
            }
            else
            {
                return ((this.MinuteSecondary == m2.MinuteSecondary) && (this.HourPrimery == m2.HourPrimery));
            }
        }
    }
}
