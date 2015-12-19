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
        public List<Candle>     NeuralNetworkRawData = new List<Candle>();
        public List<Candle>     NeuralNetworkCandlesData = new List<Candle>();
        public List<ZigZagData> NeuralNetworkZigZagData = new List<ZigZagData>();
        public List<ZigZagData> NeuralNetworkData = new List<ZigZagData>();
        public List<NNOrder>    NeuralNetworkCollection = new List<NNOrder>();
        public NeuralNetwork    NN;

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

        // Neural Network
        public int P, N              = 0;
        int     nPrecentageToTrain   = 75;
        int     nCandlesPerSample    = 21;       //48;
        int     nParamsPerCandle     = 7;
        int     numOutput            = 2;
        int     maxEpochsLoop        = 50;       // 2000
        double  learnRate            = 0.05;     // 0.05
        double  momentum             = 0.01;     // 0.01
        double  weightDecay          = 0.0001;   // 0.0001
        double  meanSquaredError     = 0.001;    // 0.020


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


            // Register Indicators // RSI > StochasticRSI > Stochastic > SMA > EMA > WMA > Awesome > TDI
            RSI             = new RSI();
            SMA             = new SMA();
            EMA             = new EMA();
            WMA             = new WMA();
            TDI             = new TDI();
            ZigZag5         = new ZigZag(5, false);
            ZigZag12        = new ZigZag(12, true);
            RSI.RegisterIndicator(this);
            SMA.RegisterIndicator(this);
            EMA.RegisterIndicator(this);
            WMA.RegisterIndicator(this);
            TDI.RegisterIndicator(this);
            ZigZag5.RegisterIndicator(this);
            ZigZag12.RegisterIndicator(this);

            // NewIndicatorValue and CompleteInitializationActions 
            this.IndicatorsList.ForEach(I => I.NewIndicatorValue());
            this.IndicatorsList.ForEach(I => I.CompleteInitializationActions());
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
                this.LastCandle.ExtraList.Add(this.TDI.TDI_Green);
                this.LastCandle.ExtraList.Add(this.TDI.TDI_Red);
                this.LastCandle.ExtraList.Add(this.TDI.TDI_Upper);
                this.LastCandle.ExtraList.Add(this.TDI.TDI_Lower);

                // Log Candle
                this.LastCandle.LogCandle(this.ParentShare, this.SMA.LowerBollinger);
                MongoDBUtils.DBEventAfterCandleFinished(this.ParentShare, this.LastCandle);

                if (this.NeuralNetworkRawData.Count == Consts.NEURAL_NETWORK_NUM_OF_TRAINING_CANDLES) { this.NeuralNetworkRawData.RemoveAt(0); }
                this.NeuralNetworkRawData.Add(this.LastCandle);

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
                this.LastCandle.StartWMA = this.WMA.Value;
                this.LastCandle.StartEMA = this.EMA.Value;
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
            this.NeuralNetworkData.Clear();
            this.NeuralNetworkData.AddRange(this.NeuralNetworkZigZagData);
            int numInput = nCandlesPerSample * nParamsPerCandle;
            int numHidden = numInput / 2;

            double[][] AllData;
            double[][] TrainData;
            double[][] TestData;
            double[][] NormalizedTraingData;
            double[][] NormalizedTestData;

            // Initialize and Train the NeuralNetwork
            NeuralNetwork nn = new NeuralNetwork(numInput, numHidden, numOutput);
            AllData = this.MakeNeuralNetworkDataMatrix(true, nCandlesPerSample, numOutput, nParamsPerCandle);
            NeuralNetwork.MakeTrainAndTestRandom(AllData, out TrainData, out TestData, nPrecentageToTrain);

            // Normalizing Data
            NormalizedTraingData = NeuralNetwork.NormalizeMatrix(TrainData, TrainData[0].Length - numOutput);
            NormalizedTestData = NeuralNetwork.NormalizeMatrix(TestData, TestData[0].Length - numOutput);

            // Save matrixes
            nn.RawTrainData = TrainData;
            nn.NormalizedTestData = NormalizedTestData;

            // Train NeuralNetwork
            nn.InitializeWeights();
            nn.Train(NormalizedTraingData, maxEpochsLoop, learnRate, momentum, weightDecay, meanSquaredError);

            // Accuracy check   
            nn.AccuracyRate = nn.Accuracy(NormalizedTestData);

            // Set the new NeuralNetwork as the Current
            this.NN = nn;
        }
        public bool NeuralNetworPredict()
        {
            double[][] arrPredictSet;
            double[][] arrNormalizePredictionMatrix;
            double nPrediction;

            if (this.NN != null) 
            {
                arrPredictSet = MakeNeuralNetworkDataMatrix(false, nCandlesPerSample, numOutput, nParamsPerCandle);
                this.NN.RawTrainData[this.NN.RawTrainData.Length - 1] = arrPredictSet[0];
                arrNormalizePredictionMatrix = NeuralNetwork.NormalizeMatrix(this.NN.RawTrainData, this.NN.RawTrainData[0].Length - numOutput);
                nPrediction = this.NN.Predict(arrNormalizePredictionMatrix[arrNormalizePredictionMatrix.Length - 1]);

                this.Candles[this.CountDec - 1].ZigZagPrediction = nPrediction;
            }

            return (false);
        }
        public double[][] MakeNeuralNetworkDataMatrix(bool bIsTrainingMatrix, int nCandlesPerSample, int nOutputCount, int nParamsPerCandle)
        {
            int nSamplesCount = this.NeuralNetworkData.Count(C => C.CandleIndex - nCandlesPerSample > -1);
            int nCandlesListStart = bIsTrainingMatrix ? 0 : this.Candles.Count - nCandlesPerSample - 1;
            int nMatrixSizeToReturn = bIsTrainingMatrix ? nSamplesCount : 1;
            double[][] arrAllSampls = new double[nMatrixSizeToReturn][];
            double[] arrSample;
            int nSampleIndex = 0;
            Candle cCurrCandle;

            if (bIsTrainingMatrix)
            {
                foreach (ZigZagData zzCurr in this.NeuralNetworkData)
                {
                    int nStart = zzCurr.CandleIndex - nCandlesPerSample;
                    
                    if (nStart > -1)
                    {
                        arrSample = new double[nCandlesPerSample * nParamsPerCandle + nOutputCount];
                        for (int nZZindex = nStart, nSampleCandleIndex = 0; nZZindex < zzCurr.CandleIndex; nZZindex++, nSampleCandleIndex += nParamsPerCandle)
                        {
                            cCurrCandle = this.NeuralNetworkCandlesData[nZZindex];
                            arrSample[nSampleCandleIndex + 0] = MathUtils.PercentChange(cCurrCandle.R_Low, cCurrCandle.R_High);
                            arrSample[nSampleCandleIndex + 1] = MathUtils.PercentChange(cCurrCandle.ExtraList[1], cCurrCandle.ExtraList[2]);
                            arrSample[nSampleCandleIndex + 2] = MathUtils.PercentChange(cCurrCandle.R_Low, cCurrCandle.ExtraList[1]);
                            arrSample[nSampleCandleIndex + 3] = MathUtils.PercentChange(cCurrCandle.ExtraList[2], cCurrCandle.R_High);
                            arrSample[nSampleCandleIndex + 4] = cCurrCandle.WMADirection ? 1 : 0;
                            arrSample[nSampleCandleIndex + 5] = cCurrCandle.EMADirection ? 1 : 0;
                            arrSample[nSampleCandleIndex + 6] = cCurrCandle.R_Close > cCurrCandle.R_Open ? 1 : 0;
                        }

                        arrSample[nCandlesPerSample * nParamsPerCandle + (int)zzCurr.Indication] = 1;
                        arrAllSampls[nSampleIndex++] = arrSample;
                    }
                }
            }
            else
            {
                int nStart = this.CountDec - 21;

                if (nStart > -1)
                {
                    arrSample = new double[nCandlesPerSample * nParamsPerCandle + nOutputCount];
                    for (int nZZindex = nStart, nSampleCandleIndex = 0; nZZindex < this.CountDec; nZZindex++, nSampleCandleIndex += nParamsPerCandle)
                    {
                        cCurrCandle = this.Candles[nZZindex];
                        arrSample[nSampleCandleIndex + 0] = MathUtils.PercentChange(cCurrCandle.R_Low, cCurrCandle.R_High);
                        arrSample[nSampleCandleIndex + 1] = MathUtils.PercentChange(cCurrCandle.ExtraList[1], cCurrCandle.ExtraList[2]);
                        arrSample[nSampleCandleIndex + 2] = MathUtils.PercentChange(cCurrCandle.R_Low, cCurrCandle.ExtraList[1]);
                        arrSample[nSampleCandleIndex + 3] = MathUtils.PercentChange(cCurrCandle.ExtraList[2], cCurrCandle.R_High);
                        arrSample[nSampleCandleIndex + 4] = cCurrCandle.WMADirection ? 1 : 0;
                        arrSample[nSampleCandleIndex + 5] = cCurrCandle.EMADirection ? 1 : 0;
                        arrSample[nSampleCandleIndex + 6] = cCurrCandle.R_Close > cCurrCandle.R_Open ? 1 : 0;
                    }

                    arrAllSampls[0] = arrSample;
                }
            }

            return (arrAllSampls);
        }
    }
}
