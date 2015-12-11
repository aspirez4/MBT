using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBTrading.Entities.Indicators
{
    public class EMA : Indicator
    {
        public CandlesList ParentCandleList = null;
        public double   Value;
        public double   Prev;
        public bool     Direction;
        public bool     PrevDirection;
        private double  EMAMultiplier;
        public double   Derivative;
        public double   PrevDerivative;
        public bool     DerivativeDierection;
        public double   MaxEMA;
        public double   MinEMA;
        public List<double> EMAList;

        public EMA()
        {
            EMAList = new List<double>();
        }


        public void RegisterIndicator(CandlesList clParentCandlesList)
        {
            // Register inicator
            this.ParentCandleList = clParentCandlesList;
            clParentCandlesList.IndicatorsList.Add(this);

            // Initialize indicator list
            this.Direction = true;
            this.PrevDirection = true;
            this.Value = clParentCandlesList.SMA.Value;
            this.EMAMultiplier = (2 / (double)(Consts.esMA_PARAMETERS_LENGTH + 1));
            int nStopIndex = Math.Max(0, clParentCandlesList.CountDec - Consts.esMA_PARAMETERS_LENGTH);
            for (int nCounter = clParentCandlesList.CountDec; nCounter > nStopIndex; nCounter--) { this.EMAList.Add(clParentCandlesList.Candles[nCounter].Close); }
        }

        public void NewIndicatorValue()
        {
            double dTempEMA = this.Value;
            double dTempDerivative = this.Derivative;
            this.PrevDirection = this.Direction;

            UpdateIndicatorValue();
            this.Prev = dTempEMA;

            if (this.Value > this.MaxEMA) { this.MaxEMA = this.Value; }

            this.EMAList.Add(this.Value);

            double dFirstEMA = this.EMAList[0];
            this.EMAList.RemoveAt(0);
            if (dFirstEMA == this.MaxEMA) { this.MaxEMA = this.EMAList.Max(); }
            else if (this.Value > this.MaxEMA) { this.MaxEMA = this.Value; }
            if (dFirstEMA == this.MinEMA) { this.MinEMA = this.EMAList.Min(); }
            else if (this.Value < this.MinEMA) { this.MinEMA = this.Value; }

            this.PrevDerivative = dTempDerivative;
            this.DerivativeDierection = this.Derivative >= this.PrevDerivative;
        }

        public void UpdateIndicatorValue()
        {
            this.Value = (this.ParentCandleList.CurrPrice - this.Prev) * this.EMAMultiplier + this.Prev;
            this.Direction = this.Value > this.Prev;

            this.Derivative = this.Value / this.Prev;
            this.DerivativeDierection = this.Derivative >= this.PrevDerivative;
        }

        public void BeforeNewCandleActions(Candle cNewCandle)
        {
            this.Value = this.ParentCandleList.SMA.Value;
            this.Prev = this.ParentCandleList.SMA.Value;
            this.MaxEMA = this.EMAList.Max();
            this.MinEMA = this.EMAList.Min();
            this.Derivative = this.Value / this.Prev;
            this.PrevDerivative = this.Derivative;
        }


        public void CompleteInitializationActions()
        {
        }
    }
}
