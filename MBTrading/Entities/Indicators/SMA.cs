using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBTrading.Entities.Indicators
{
    public class SMA : Indicator
    {
        public CandlesList ParentCandleList = null;
        public double Value;
        public double Prev;
        public double UpperEnvelope;
        public double LowerEnvelope;
        public double UpperBollinger;
        public double LowerBollinger;
        public double CandleSizeAVG;
        public List<Candle> SMAList;

        public SMA()
        {
            SMAList = new List<Candle>();
        }

        public void NewIndicatorValue()
        {
            this.Prev = this.Value;
            this.UpdateIndicatorValue();
        }

        public void UpdateIndicatorValue()
        {
            double dFirstStandardDeviationOperand = 0;
            double dSecondStandardDeviationOperand = 0;
            double dAVG = 0;
            double dTemp;
            double dSizeTemp = 0;

            // Calculate avarage and StandardDeviation
            foreach (Candle cCurCandle in this.SMAList)
            {
                // SMA
                dTemp = (cCurCandle.Low + cCurCandle.High) / 2;
                dAVG += dTemp;
                dFirstStandardDeviationOperand += Math.Pow(dTemp, 2);
                dSizeTemp += cCurCandle.High - cCurCandle.Low;
            }

            dSecondStandardDeviationOperand = dAVG;
            this.CandleSizeAVG = dSizeTemp / this.SMAList.Count;
            this.Value = dAVG / this.SMAList.Count;
            double dStandardDeviation = Math.Sqrt(Math.Pow(this.Value, 2) +
                                                  ((dFirstStandardDeviationOperand -
                                                   (dSecondStandardDeviationOperand * 2 * this.Value)) /
                                                   this.SMAList.Count));

            // Calculate Envelope and Bollinger
            this.LowerEnvelope = this.Value - (this.Value * Consts.esMA_PARAMETERS_PERCENTAGE * 0.01);
            this.UpperEnvelope = this.Value + (this.Value * Consts.esMA_PARAMETERS_PERCENTAGE * 0.01);
            this.LowerBollinger = this.Value - (dStandardDeviation * 2);
            this.UpperBollinger = this.Value + (dStandardDeviation * 2);
        }

        public void RegisterIndicator(CandlesList clParentCandlesList)
        {
            // Register inicator
            this.ParentCandleList = clParentCandlesList;
            clParentCandlesList.IndicatorsList.Add(this);

            // Initialize indicator list
            int nStopIndex = Math.Max(0, clParentCandlesList.CountDec - Consts.esMA_PARAMETERS_LENGTH);
            for (int nCounter = clParentCandlesList.CountDec; nCounter > nStopIndex; nCounter--) 
            {
                this.SMAList.Add(clParentCandlesList.Candles[nCounter]);  
            }
        }

        public void BeforeNewCandleActions(Candle cNewCandle)
        {
            this.SMAList.RemoveAt(0);
            this.SMAList.Add(cNewCandle);
        }

        public void CompleteInitializationActions()
        {
            this.Prev = this.Value;
        }
    }
}
