using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBTrading.Entities.Indicators
{
    public class Stochastic : Indicator
    {
        public CandlesList ParentCandleList = null;
        public double Value;
        public double Prev;

        public void RegisterIndicator(CandlesList clParentCandlesList)
        {
            // Register inicator
            this.ParentCandleList = clParentCandlesList;
            clParentCandlesList.IndicatorsList.Add(this);

            // Initialize indicator
            this.Value = 100 * ((clParentCandlesList.Candles[clParentCandlesList.CountDec].Close - clParentCandlesList.LowestPrice) / (clParentCandlesList.HighestPrice - clParentCandlesList.LowestPrice));
            this.Prev = this.Value;
        }

        public void NewIndicatorValue()
        {
            this.Prev = this.Value;
            this.UpdateIndicatorValue();
        }

        public void UpdateIndicatorValue()
        {
            this.Value = 100 * ((this.ParentCandleList.CurrPrice - this.ParentCandleList.LowestPrice) / (this.ParentCandleList.HighestPrice - this.ParentCandleList.LowestPrice));
        }

        public void BeforeNewCandleActions(Candle cNewCandle)
        {
        }


        public void CompleteInitializationActions()
        {
        }
    }
}
