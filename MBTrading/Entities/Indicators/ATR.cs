using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MBTrading.Entities.Indicators
{
    public class ATR : Indicator
    {
        private CandlesList ParentCandleList = null;

        public double ChandelierLongValue;
        public double ChandelierShortValue;
        public double ATR_Value;
        private double  TR_Dec;
        private int Length;
        private double Multiplier;
        private List<double> ATRList;
        private double dHighestHigh;
        private double dLowestLow;

        public ATR(int nLength, double dMultiplier)
        {
            Multiplier = dMultiplier;
            Length = nLength;
            dLowestLow = double.MaxValue;
            dHighestHigh = double.MinValue;
            ATRList = new List<double>();
        }

        public double GetTR(Candle cCurrCandle, Candle cPrevCandle)
        {
            return (Math.Max(Math.Max(
                cCurrCandle.R_High - cPrevCandle.R_Close,
                cPrevCandle.R_Close - cCurrCandle.R_Low),
                cCurrCandle.R_High - cCurrCandle.R_Low));
        }
        private void FindHighestAndLowest()
        {
            dLowestLow = double.MaxValue;
            dHighestHigh = double.MinValue;
            int nStartOffset = this.ParentCandleList.Candles.Count < this.Length ? this.ParentCandleList.Candles.Count : this.Length;
            for (int nIndex = this.ParentCandleList.Candles.Count - nStartOffset; nIndex < this.ParentCandleList.Candles.Count; nIndex++)
            {
                if (this.dHighestHigh < this.ParentCandleList.Candles[nIndex].R_High)
                    this.dHighestHigh = this.ParentCandleList.Candles[nIndex].R_High;
                if (this.dLowestLow > this.ParentCandleList.Candles[nIndex].R_Low)
                    this.dLowestLow = this.ParentCandleList.Candles[nIndex].R_Low;
            }
        }
        public void RegisterIndicator(CandlesList clParentCandlesList)
        {
            // Register inicator
            this.ParentCandleList = clParentCandlesList;
            clParentCandlesList.IndicatorsList.Add(this);


            // Initialize indicator list
            int nStopIndex = Math.Max(1, clParentCandlesList.CountDec - Length);
            for (int nCounter = clParentCandlesList.CountDec - 1; nCounter > nStopIndex; nCounter--) { this.ATRList.Add(GetTR(clParentCandlesList.Candles[nCounter], clParentCandlesList.Candles[nCounter - 1])); }


            this.TR_Dec = this.ATRList.Average();
            FindHighestAndLowest();
        }

        public void NewIndicatorValue()
        {
            this.ATRList.Add(GetTR(this.ParentCandleList.Candles[this.ParentCandleList.CountDec - 1], this.ParentCandleList.Candles[this.ParentCandleList.CountDec - 2]));
            this.ATRList.Remove(0);
            this.TR_Dec = this.ATRList.Average();

            UpdateIndicatorValue();
        }

        public void UpdateIndicatorValue()
        {
            if (this.dHighestHigh < this.ParentCandleList.LastCandle.R_High)
                this.dHighestHigh = this.ParentCandleList.LastCandle.R_High;
            if (this.dLowestLow > this.ParentCandleList.LastCandle.R_Low)
                this.dLowestLow = this.ParentCandleList.LastCandle.R_Low;

            this.ATR_Value = ((this.TR_Dec * (this.Length - 1)) +
                             GetTR(this.ParentCandleList.Candles[this.ParentCandleList.CountDec], this.ParentCandleList.Candles[this.ParentCandleList.CountDec - 1])) 
                             / this.Length;

            this.ChandelierLongValue = this.dHighestHigh - (this.ATR_Value * this.Multiplier);
            this.ChandelierShortValue = this.dLowestLow + (this.ATR_Value * this.Multiplier);
        }

        public void BeforeNewCandleActions(Candle cNewCandle)
        {
            FindHighestAndLowest();
        }


        public void CompleteInitializationActions()
        {
        }
    }
}
