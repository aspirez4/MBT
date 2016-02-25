using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBTrading.Entities.Indicators
{
    public class TDI : Indicator
    {
        public CandlesList ParentCandleList = null;
        public double PrevTDI_Green;
        public double PrevTDI_Red;
        public double TDI_Green;
        public double TDI_Red;
        public double TDI_Upper;
        public double TDI_Lower;
        public double TDI_Mid;
        public List<double> TDIRSIList;

        public void RegisterIndicator(CandlesList clParentCandlesList)
        {
            // Register inicator
            this.ParentCandleList = clParentCandlesList;
            clParentCandlesList.IndicatorsList.Add(this);

            // Initialize indicator list
            this.TDIRSIList = new List<double>();
            for (int nCounter = 0; nCounter < 34; nCounter++) { this.TDIRSIList.Add(clParentCandlesList.RSI.Value); }
        }

        public void NewIndicatorValue()
        {
            this.PrevTDI_Green = this.TDI_Green;
            this.PrevTDI_Red = this.TDI_Red;
            this.TDIRSIList.RemoveAt(0);
            this.TDIRSIList.Add(this.ParentCandleList.RSI.Value);
            this.UpdateIndicatorValue();
        }

        public void UpdateIndicatorValue()
        {
            int nGreenPeriod = 2;
            int nRedPeriod = 7;
            double dTempTDI_Red = 0;
            int dTDIRSIListCountDec = this.TDIRSIList.Count - 1;
            this.TDIRSIList[dTDIRSIListCountDec] = this.ParentCandleList.RSI.Value;

            // Sum last nRedPeriod RSI
            for (int nTDIRSIIndex = dTDIRSIListCountDec; nTDIRSIIndex > dTDIRSIListCountDec - nRedPeriod; nTDIRSIIndex--)
            {
                dTempTDI_Red += this.TDIRSIList[nTDIRSIIndex];
            }
            this.TDI_Red = dTempTDI_Red / nRedPeriod;
            this.TDI_Green = (this.TDIRSIList[dTDIRSIListCountDec] + this.TDIRSIList[dTDIRSIListCountDec]) / nGreenPeriod;


            double dFirstStandardDeviationOperand = 0;
            double dSecondStandardDeviationOperand = 0;
            double dAVG = 0;


            // Calculate avarage and StandardDeviation
            foreach (double dCurrRSI in this.TDIRSIList)
            {
                dAVG += dCurrRSI;
                dFirstStandardDeviationOperand += Math.Pow(dCurrRSI, 2);
            }

            dSecondStandardDeviationOperand = dAVG;

            double RSI_MA = dAVG / this.TDIRSIList.Count;
            double dStandardDeviation = Math.Sqrt(Math.Pow(RSI_MA, 2) +
                                                  ((dFirstStandardDeviationOperand -
                                                   (dSecondStandardDeviationOperand * 2 * RSI_MA)) /
                                                   this.TDIRSIList.Count));

            // Calculate TDIRSI Envelope
            this.TDI_Lower = RSI_MA - (dStandardDeviation * 1.6185);
            this.TDI_Upper = RSI_MA + (dStandardDeviation * 1.6185);
            this.TDI_Mid = (this.TDI_Lower + this.TDI_Upper) / 2;
        }

        public void BeforeNewCandleActions(Candle cNewCandle)
        {
        }


        public void CompleteInitializationActions()
        {
        }
    }
}
