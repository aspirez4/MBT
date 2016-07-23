using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBTrading.Entities.Indicators
{
    public class WMA : Indicator
    {
        public CandlesList ParentCandleList = null;
        public double Value;
        public double Prev;
        public bool   Direction;
        public bool   PrevDirection;
        private int WMAParametersDenominator;

        public void RegisterIndicator(CandlesList clParentCandlesList)
        {
            // Register inicator
            this.ParentCandleList = clParentCandlesList;
            clParentCandlesList.IndicatorsList.Add(this);

            // Initialize indicator
            for (int nCounter = 1; nCounter <= Consts.WMA_PARAMETERS_LENGTH; nCounter++) { this.WMAParametersDenominator += nCounter; }           
        }

        public void NewIndicatorValue()
        {
            this.Prev = this.Value;
            this.PrevDirection = this.Direction;
            this.UpdateIndicatorValue();
        }

        public void UpdateIndicatorValue()
        {
            int dIndex = 1;
            this.Value = 0;
            for (int nCounter = Consts.WMA_PARAMETERS_LENGTH; nCounter > 0; nCounter--)
            {
                this.Value += this.ParentCandleList.Candles[this.ParentCandleList.Count - dIndex].Close * nCounter;
                dIndex++;
            }
            this.Value /= this.WMAParametersDenominator;
            this.Direction = this.Value > this.Prev;
        }

        public void BeforeNewCandleActions(Candle cNewCandle)
        {
        }


        public void CompleteInitializationActions()
        {
            this.Prev = this.Value;
            this.Direction = true;
            this.PrevDirection = true;
        }
    }
}
