using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MBTrading.Entities.Indicators
{
    public class StochasticRSI : Indicator
    {
        public CandlesList ParentCandleList = null;
        public double Value;
        public double Prev;
        public double AVGStochasticRSI_K;
        public double AVGStochasticRSI_D;
        public double PrevAVGStochasticRSI_K;
        public double PrevAVGStochasticRSI_D;
        private int StochasticParameterKsmoothingLengthDec;
        private int StochasticParameterDsmoothingLengthDec;
        public List<double> Stochastic_K_SmoothingList;
        public List<double> Stochastic_D_SmoothingList;

        public StochasticRSI()
        {
            this.Stochastic_K_SmoothingList = new List<double>();
            this.Stochastic_D_SmoothingList = new List<double>();
        }
        public void RegisterIndicator(CandlesList clParentCandlesList)
        {
            // Register inicator
            this.ParentCandleList = clParentCandlesList;
            clParentCandlesList.IndicatorsList.Add(this);

            // Initialize indicator
            double Stochastic = 100 * ((clParentCandlesList.Candles[clParentCandlesList.CountDec].Close - clParentCandlesList.LowestPrice) / (clParentCandlesList.HighestPrice - clParentCandlesList.LowestPrice));
            this.Value = 50;
            this.Prev = 50; 
            this.AVGStochasticRSI_K = Stochastic;
            this.AVGStochasticRSI_D = Stochastic;
            this.PrevAVGStochasticRSI_K = this.AVGStochasticRSI_K;
            this.PrevAVGStochasticRSI_D = this.AVGStochasticRSI_D;
            this.StochasticParameterKsmoothingLengthDec = Consts.STOCHASTIC_PARAMETERS_K_SMOOTHING_LENGTH - 1;
            this.StochasticParameterDsmoothingLengthDec = Consts.STOCHASTIC_PARAMETERS_D_SMOOTHING_LENGTH - 1;
            for (int nCounter = 0; nCounter < Consts.STOCHASTIC_PARAMETERS_K_SMOOTHING_LENGTH; nCounter++) { this.Stochastic_K_SmoothingList.Add(this.Value); }
            for (int nCounter = 0; nCounter < Consts.STOCHASTIC_PARAMETERS_D_SMOOTHING_LENGTH; nCounter++) { this.Stochastic_D_SmoothingList.Add(this.Value); }
        }

        public void NewIndicatorValue()
        {
            this.Prev = this.Value;
            this.Value = 100 * ((this.ParentCandleList.RSI.Value - this.ParentCandleList.RSI.LowestRSI) / (this.ParentCandleList.RSI.HighestRSI - this.ParentCandleList.RSI.LowestRSI));

            this.Stochastic_K_SmoothingList.Add(this.Value);
            this.Stochastic_K_SmoothingList.RemoveAt(0);
            this.PrevAVGStochasticRSI_K = this.AVGStochasticRSI_K;
            this.AVGStochasticRSI_K = this.Stochastic_K_SmoothingList.Average();

            this.Stochastic_D_SmoothingList.Add(this.AVGStochasticRSI_K);
            this.Stochastic_D_SmoothingList.RemoveAt(0);
            this.PrevAVGStochasticRSI_D = this.AVGStochasticRSI_D;
            this.AVGStochasticRSI_D = this.Stochastic_D_SmoothingList.Average();
        }

        public void UpdateIndicatorValue()
        {
            this.Value = 100 * ((this.ParentCandleList.RSI.Value - this.ParentCandleList.RSI.LowestRSI) / (this.ParentCandleList.RSI.HighestRSI - this.ParentCandleList.RSI.LowestRSI));

            this.Stochastic_K_SmoothingList[this.StochasticParameterKsmoothingLengthDec] = (this.Value);
            this.AVGStochasticRSI_K = this.Stochastic_K_SmoothingList.Average();
            this.Stochastic_D_SmoothingList[this.StochasticParameterDsmoothingLengthDec] = (this.AVGStochasticRSI_K);
            this.AVGStochasticRSI_D = this.Stochastic_D_SmoothingList.Average();
        }

        public void BeforeNewCandleActions(Candle cNewCandle)
        {
        }


        public void CompleteInitializationActions()
        {
        }
    }
}
