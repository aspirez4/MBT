using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBTrading.Entities.Indicators
{
    public class RSI : Indicator
    {
        public  CandlesList ParentCandleList = null;
        public  double  Value;
        public  double  RS;
        public  double  AVGGain;
        public  double  AVGLoss;
        public  double  LowestRSI;
        public  double  HighestRSI;
        private int     RSIParameters_LengthDec;
        public List<double> RSIList;

        public RSI()
        {
            RSIList = new List<double>();
        }

        public void RegisterIndicator(CandlesList clParentCandlesList)
        {
            // Register inicator
            this.ParentCandleList = clParentCandlesList;
            clParentCandlesList.IndicatorsList.Add(this);

            #region RS Parameters
            
            this.LowestRSI = double.MaxValue;
            this.HighestRSI = double.MinValue;
            double dChangingRate;
            Candle cCurrCandle;
            for (int nStochasticIndex = clParentCandlesList.CountDec; nStochasticIndex > clParentCandlesList.CountDec - Consts.STOCHASTIC_PARAMETERS_LENGTH; nStochasticIndex--)
            {
                cCurrCandle = clParentCandlesList.Candles[nStochasticIndex];
                if (cCurrCandle.High > clParentCandlesList.HighestPrice)
                {
                    clParentCandlesList.HighestPrice = cCurrCandle.High;
                }

                if (cCurrCandle.Low < clParentCandlesList.LowestPrice)
                {
                    clParentCandlesList.LowestPrice = cCurrCandle.Low;
                }
            }
            for (int nRSIIndex = clParentCandlesList.CountDec; nRSIIndex > clParentCandlesList.CountDec - Consts.RSI_PARAMETERS_LENGTH; nRSIIndex--)
            {
                cCurrCandle = clParentCandlesList.Candles[nRSIIndex];
                dChangingRate = cCurrCandle.Close - cCurrCandle.Open;
                if (dChangingRate > 0)
                {
                    this.AVGGain += dChangingRate;
                }
                else
                {
                    this.AVGLoss -= dChangingRate;
                }
            }
            #endregion

            this.RSIParameters_LengthDec = Consts.RSI_PARAMETERS_LENGTH - 1;
            this.AVGGain /= Consts.RSI_PARAMETERS_LENGTH;
            this.AVGLoss /= Consts.RSI_PARAMETERS_LENGTH;
            this.RS = this.AVGGain / this.AVGLoss;
            this.Value = 100 - (100 / (1 + this.RS));

            // Initialize indicator list
            for (int nCounter = 0; nCounter < Consts.STOCHASTIC_PARAMETERS_LENGTH; nCounter++) 
            { 
                this.RSIList.Add(this.Value); 
            }
        }

        public void NewIndicatorValue()
        {
            double dLastChangingRate = this.ParentCandleList.Candles[this.ParentCandleList.CountDec - 1].Close -
                                       this.ParentCandleList.Candles[this.ParentCandleList.CountDec - 2].Close;

            if (dLastChangingRate > 0)
            {
                this.AVGGain = ((this.AVGGain * (this.RSIParameters_LengthDec)) + dLastChangingRate) / Consts.RSI_PARAMETERS_LENGTH;
                this.AVGLoss = ((this.AVGLoss * (this.RSIParameters_LengthDec))) / Consts.RSI_PARAMETERS_LENGTH;
            }
            else
            {
                this.AVGLoss = ((this.AVGLoss * (this.RSIParameters_LengthDec)) - dLastChangingRate) / Consts.RSI_PARAMETERS_LENGTH;
                this.AVGGain = ((this.AVGGain * (this.RSIParameters_LengthDec))) / Consts.RSI_PARAMETERS_LENGTH;
            }

            this.UpdateIndicatorValue();
            this.RSIList.Add(this.Value);
            double dFirstRSI = this.RSIList[0];
            this.RSIList.RemoveAt(0);
            if (dFirstRSI == this.HighestRSI) { this.HighestRSI = this.RSIList.Max(); }
            if (dFirstRSI == this.LowestRSI) { this.LowestRSI = this.RSIList.Min(); }
        }

        public void UpdateIndicatorValue()
        {
            double dLastChangingRate = this.ParentCandleList.Candles[this.ParentCandleList.CountDec].Close -
                                       this.ParentCandleList.Candles[this.ParentCandleList.CountDec - 1].Close;
            double dAVGGainTemp, dAVGLossTemp;

            if (dLastChangingRate > 0)
            {
                dAVGGainTemp = ((this.AVGGain * (this.RSIParameters_LengthDec)) + dLastChangingRate) / Consts.RSI_PARAMETERS_LENGTH;
                dAVGLossTemp = ((this.AVGLoss * (this.RSIParameters_LengthDec))) / Consts.RSI_PARAMETERS_LENGTH;
            }
            else
            {
                dAVGLossTemp = ((this.AVGLoss * (this.RSIParameters_LengthDec)) - dLastChangingRate) / Consts.RSI_PARAMETERS_LENGTH;
                dAVGGainTemp = ((this.AVGGain * (this.RSIParameters_LengthDec))) / Consts.RSI_PARAMETERS_LENGTH;
            }


            try
            {
                this.RS = dAVGGainTemp / dAVGLossTemp;
                this.Value = 100 - (100 / (1 + this.RS));
            }
            catch
            {
                this.RS = -1;
                this.Value = 100;
            }

            if (this.Value > this.HighestRSI) { this.HighestRSI = this.Value; }
            if (this.Value < this.LowestRSI)  { this.LowestRSI = this.Value; }
        }

        public void BeforeNewCandleActions(Candle cNewCandle)
        {
        }


        public void CompleteInitializationActions()
        {
        }
    }
}
