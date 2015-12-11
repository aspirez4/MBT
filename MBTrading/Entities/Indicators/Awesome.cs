using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBTrading.Entities.Indicators
{
    public class Awesome : Indicator
    {
        public CandlesList ParentCandleList = null;
        public double Value;
        public double Prev;
        private List<Candle> AwesomeMidSMAShortList;
        private List<Candle> AwesomeMidSMALongList;

        public void RegisterIndicator(CandlesList clParentCandlesList)
        {
            // Register inicator
            this.ParentCandleList = clParentCandlesList;
            clParentCandlesList.IndicatorsList.Add(this);

            // Initialize indicator list
            AwesomeMidSMAShortList = new List<Candle>();
            AwesomeMidSMALongList = new List<Candle>();
            for (int nCounter = 0; nCounter < 34; nCounter++) { this.AwesomeMidSMAShortList.Add(clParentCandlesList.Candles[clParentCandlesList.CountDec]); }
            for (int nCounter = 0; nCounter < 5; nCounter++) { this.AwesomeMidSMALongList.Add(clParentCandlesList.Candles[clParentCandlesList.CountDec]); }
        }

        public void NewIndicatorValue()
        {
            this.Prev = this.Value;
            this.UpdateIndicatorValue();
        }

        public void UpdateIndicatorValue()
        {
            this.Value = ((this.AwesomeMidSMAShortList.Average(C => C.High + C.Low)) / 2) -
                         ((this.AwesomeMidSMALongList.Average(C => C.High + C.Low)) / 2);
        }

        public void BeforeNewCandleActions(Candle cNewCandle)
        {
            this.AwesomeMidSMALongList.RemoveAt(0);
            this.AwesomeMidSMAShortList.RemoveAt(0);
            this.AwesomeMidSMALongList.Add(cNewCandle);
            this.AwesomeMidSMAShortList.Add(cNewCandle);
        }


        public void CompleteInitializationActions()
        {
            this.Prev = this.Value;
        }
    }
}
