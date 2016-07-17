using System;
using System.Collections.Generic;
using System.Linq;
using MBTrading.Utils;
using System.IO;
using MBTrading.Entities;
using MBTrading.Entities.Indicators;

namespace MBTrading
{
    public class CandlesList
    {
        // Neural Network
        public List<double>                 NeuralNetworkRawData = new List<double>();
        public NeuralNetwork                NN;

        // CandlesList
        public Share        ParentShare;
        public List<Candle> Candles;
        public Candle       LastCandle;
        public Candle       PrevCandle;
        public int          MinuteCandles;
        public bool         Primary;
        public double       CurrPrice;
        public int          Count;
        public int          CountDec;
        public double       LowestPrice;
        public double       HighestPrice; 

        // Indicators - Previous indicators refers to the last bit of the previous candle
        // RSI > StochasticRSI > Stochastic > SMA > EMA > WMA > Awesome > TDI > ZigZag
        public List<Indicator> IndicatorsList = new List<Indicator>();
        public RSI              RSI;
        public SMA              SMA;
        public EMA              EMA;
        public WMA              WMA;
        public TDI              TDI;
        public ZigZag           ZigZag5;
        public ZigZag           ZigZag12;
        public ATR              ATR;

        // Neural Network
        public int P, N              = 0;
        public int nPossitiveIndex   = 0;



        // CandleList
        public CandlesList(List<Candle> lstInitializeList, Share sParentShare, bool bPrimary)
        {
            this.ParentShare                    = sParentShare;
            this.Candles                        = lstInitializeList;
            this.Primary                        = bPrimary;
            this.MinuteCandles                  = this.Primary ? Consts.MINUTE_CANDLES_PRIMARY : Consts.MINUTE_CANDLES_SECONDARY;
            this.Count                          = lstInitializeList.Count;
            this.CountDec                       = this.Count - 1;
            this.LastCandle                     = this.Candles[this.CountDec];
            this.PrevCandle                     = this.Candles[this.CountDec - 1];
            this.LowestPrice                    = double.MaxValue;
            this.HighestPrice                   = double.MinValue;

            // Register Indicators // RSI > StochasticRSI > Stochastic > SMA > EMA > WMA > Awesome > TDI > ZigZag
            RSI             = new RSI();
            SMA             = new SMA();
            EMA             = new EMA();
            WMA             = new WMA();
            TDI             = new TDI();
            ZigZag5         = new ZigZag(5, false);
            ZigZag12        = new ZigZag(12, true);
            ATR             = new ATR(20, 3);
            RSI.RegisterIndicator(this);
            SMA.RegisterIndicator(this);
            EMA.RegisterIndicator(this);
            WMA.RegisterIndicator(this);
            TDI.RegisterIndicator(this);
            ZigZag5.RegisterIndicator(this);
            ZigZag12.RegisterIndicator(this);
            ATR.RegisterIndicator(this);

            // NewIndicatorValue and CompleteInitializationActions 
            this.IndicatorsList.ForEach(I => I.NewIndicatorValue());
            this.IndicatorsList.ForEach(I => I.CompleteInitializationActions());
            for (int nChankCounter = 0; nChankCounter < Consts.NEURAL_NETWORK_CHANK_SIZE; nChankCounter++) this.NeuralNetworkRawData.Add(1);
        }
        public bool AddOrUpdatePrice(MarketData mdCurrMarketData)
        {
            // Updating the HighestPrice and LowestPrice to be The LastCandle's prices if justified
            if (mdCurrMarketData.Value > this.HighestPrice) { this.HighestPrice = mdCurrMarketData.Value; }
            if (mdCurrMarketData.Value < this.LowestPrice)  { this.LowestPrice = mdCurrMarketData.Value; }


            // Get the relevant candle of the share. -> If found, update the candle
            if (this.LastCandle.StartDate.Compare(mdCurrMarketData.Time, this.Primary))
            {
                // Update candle
                this.LastCandle.UpdateCandle(mdCurrMarketData);
                this.CurrPrice = this.LastCandle.Bid;

                // Update indicators
                this.IndicatorsList.ForEach(I => I.UpdateIndicatorValue());

                // Return - "NotNew" indication
                return (false);
            }
            // Else - it's new candle - Add it to share
//          else if (mdCurrMarketData.Price != -1)
            else if (mdCurrMarketData.DataType == MarketDataType.Ask)
            {
                this.LastCandle.WMADirection = this.WMA.Direction;
                this.LastCandle.EMADirection = this.EMA.Direction;
                this.LastCandle.EndWMA = this.WMA.Value;
                this.LastCandle.EndEMA = this.EMA.Value;
                this.LastCandle.EndTDI_Green = this.TDI.TDI_Green;
                this.LastCandle.EndTDI_Red = this.TDI.TDI_Red;
                this.LastCandle.EndTDI_Mid = this.TDI.TDI_Mid;
                this.LastCandle.EndTDI_Upper = this.TDI.TDI_Upper;
                this.LastCandle.EndTDI_Lower = this.TDI.TDI_Lower;

                this.LastCandle.ExtraList.Add(this.SMA.Value);
                this.LastCandle.ExtraList.Add(this.SMA.LowerBollinger);
                this.LastCandle.ExtraList.Add(this.SMA.UpperBollinger);
                this.LastCandle.ExtraList.Add(this.RSI.Value);
                //this.LastCandle.ExtraList.Add(this.Stochastic.Value);
                //this.LastCandle.ExtraList.Add(this.StochasticRSI.Value);
                //this.LastCandle.ExtraList.Add(this.Awesome.Value);
                this.LastCandle.ExtraList.Add(this.ATR.ShortValue);
                this.LastCandle.ExtraList.Add(this.ATR.LongValue);
                this.LastCandle.ExtraList.Add(this.TDI.TDI_Green);
                this.LastCandle.ExtraList.Add(this.TDI.TDI_Red);
                this.LastCandle.ExtraList.Add(this.TDI.TDI_Upper);
                this.LastCandle.ExtraList.Add(this.TDI.TDI_Lower);
                

                // Log Candle
                this.LastCandle.LogCandle(this.ParentShare, this.SMA.LowerBollinger);
                MongoDBUtils.DBEventAfterCandleFinished(this.ParentShare, this.LastCandle);

                // if (this.NeuralNetworkRawData.Count == Consts.NEURAL_NETWORK_NUM_OF_TRAINING_CANDLES) { this.NeuralNetworkRawData.RemoveAt(0); }
                this.NeuralNetworkRawData.RemoveAt(0);
                this.NeuralNetworkRawData.Add(Math.Log(this.Candles[this.CountDec].R_Close / 
                                                       this.Candles[this.CountDec - Consts.NEURAL_NETWORK_PREDICTION_INTERVAL].R_Close, Math.E));

                // New candle
                Candle cCurrCandle = new Candle(mdCurrMarketData.Time,
                                                this.LastCandle,
                                                mdCurrMarketData.Value,
                                                mdCurrMarketData.Value,
                                                mdCurrMarketData.Value,
                                                mdCurrMarketData.Value,
                                                mdCurrMarketData.Value,
                                                mdCurrMarketData.Value,
                                                this.LastCandle.LastVolume,
                                                Consts.WorkOffLineMode ? this.ParentShare.OffLineCandleIndex + 1: this.ParentShare.CandleIndex + 1);

                // CandleList New Candle updates
                this.Candles.RemoveAt(0);
                Candle cFirstCandle = this.Candles[0];
                if (cFirstCandle.High == this.HighestPrice) { this.HighestPrice = this.Candles.Max(C => C.High); }
                if (cFirstCandle.Low == this.LowestPrice) { this.LowestPrice = this.Candles.Min(C => C.Low); }
                this.Candles.Add(cCurrCandle);
                this.PrevCandle = this.LastCandle;
                this.LastCandle = cCurrCandle;
                this.CurrPrice = this.LastCandle.Bid;

                // BeforeNewCandleActions
                this.IndicatorsList.ForEach(I => I.BeforeNewCandleActions(cCurrCandle));

                // NewIndicatorValue     
                this.IndicatorsList.ForEach(I => I.NewIndicatorValue());;

                // Update candles 'start' indicator flags
                this.LastCandle.StartWMA        = this.WMA.Value;
                this.LastCandle.StartEMA        = this.EMA.Value;
                this.LastCandle.StartTDI_Green  = this.TDI.TDI_Green;
                this.LastCandle.StartTDI_Red    = this.TDI.TDI_Red;

                // Return - "New" indication
                return (true);
            }
            else
            {
                return (false);
            }
        }


        // NeuralNetwork
        public void NeuralNetworkActivate()
        {
            this.NN = new NeuralNetwork(Consts.NEURAL_NETWORK_MA_LENGTH, this.NeuralNetworkRawData, this.ParentShare.Symbol);
            string strTrainResult = this.NN.Train();

            if (strTrainResult == null)
            {
                this.NN = null;
            }
            else
            {
                int nLast1 = strTrainResult.LastIndexOf(',');
                int nLast2 = strTrainResult.LastIndexOf('[');
                string strTrainingErrors = strTrainResult.Remove(0, Math.Max(nLast1, nLast2) + 1);
                this.NN.ErrorRate = double.Parse(strTrainingErrors.Remove(strTrainingErrors.Length - 2, 2));
            }
        }
        public double NeuralNetworPredict(double dValue, double dValueMA)
        {
            double dToReturn = -100;

            if ((this.NN != null) && (this.NN.ErrorRate < 1))
            {
                dToReturn = this.NN.Predict(dValue, dValueMA);
            }

            return (dToReturn);
        }
    }
}
