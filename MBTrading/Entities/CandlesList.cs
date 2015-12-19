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
        public List<Candle>  NeuralNetworkRawData = new List<Candle>();
        public List<NNOrder> NeuralNetworkSelfAwarenessData =  new List<NNOrder>();
        public List<NNOrder> NeuralNetworkSelfAwarenessCollection = new List<NNOrder>();
        public NeuralNetwork NNStrategy;
        public NeuralNetwork NNOther;

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
        public ZigZag           ZigZag10;

        // Neural Network
        public int P, N = 0;
        int nPrecentageToTrain      = 75;
        int nCandlesPerSample       = 10;       //48;
        int nParamsPerCandle        = 18;
        int nOutcomeIntervalLength  = 36;
        int numOutput               = 2;
        int maxEpochsLoop           = 10;       // 2000
        double learnRate            = 0.05;     // 0.05
        double momentum             = 0.01;     // 0.01
        double weightDecay          = 0.0001;   // 0.0001
        double meanSquaredError     = 0.001;    // 0.020


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
            ZigZag5         = new ZigZag(5);
            ZigZag10        = new ZigZag(12);
            RSI.RegisterIndicator(this);
            SMA.RegisterIndicator(this);
            EMA.RegisterIndicator(this);
            WMA.RegisterIndicator(this);
            TDI.RegisterIndicator(this);
            ZigZag5.RegisterIndicator(this);
            ZigZag10.RegisterIndicator(this);

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
        

        public void NeuralNetworkSelfAwarenessActivate()
        {
            this.NeuralNetworkSelfAwarenessData.Clear();
            this.NeuralNetworkSelfAwarenessData.AddRange(this.NeuralNetworkSelfAwarenessCollection);
            int numInput = nCandlesPerSample * nParamsPerCandle;
            int numHidden = numInput / 2;

            double[][] AllData;
            double[][] TrainData;
            double[][] TestData;
            double[][] NormalizedTraingData;
            double[][] NormalizedTestData;

            // Initialize and Train the NeuralNetwork
            NeuralNetwork nn = new NeuralNetwork(numInput, numHidden, numOutput);
            AllData = this.MakeNeuralNetworkDataMatrix4(true, nCandlesPerSample, numOutput, nParamsPerCandle, nOutcomeIntervalLength, true);
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
            this.NNStrategy = nn;
        }
        public bool NeuralNetworSelfAwarenessPredict(double dPossitiveRate)
        {
            double[][] arrPredictSet;
            double[][] arrNormalizePredictionMatrix;
            double nPrediction;

            if (this.NNStrategy != null) 
            {
                arrPredictSet = MakeNeuralNetworkDataMatrix4(false, nCandlesPerSample, numOutput, nParamsPerCandle, nOutcomeIntervalLength, true);
                this.NNStrategy.RawTrainData[this.NNStrategy.RawTrainData.Length - 1] = arrPredictSet[0];
                arrNormalizePredictionMatrix = NeuralNetwork.NormalizeMatrix(this.NNStrategy.RawTrainData, this.NNStrategy.RawTrainData[0].Length - numOutput);
                nPrediction = this.NNStrategy.Predict(arrNormalizePredictionMatrix[arrNormalizePredictionMatrix.Length - 1]);

                this.Candles[this.CountDec - 1].ProfitPredictionStrategy = nPrediction;
            }

            if (this.NNOther != null)
            {
                arrPredictSet = MakeNeuralNetworkDataMatrix4(false, nCandlesPerSample, numOutput, nParamsPerCandle, nOutcomeIntervalLength, false);
                this.NNOther.RawTrainData[this.NNOther.RawTrainData.Length - 1] = arrPredictSet[0];
                arrNormalizePredictionMatrix = NeuralNetwork.NormalizeMatrix(this.NNOther.RawTrainData, this.NNOther.RawTrainData[0].Length - numOutput);
                nPrediction = this.NNOther.Predict(arrNormalizePredictionMatrix[arrNormalizePredictionMatrix.Length - 1]);

                this.Candles[this.CountDec - 1].ProfitPredictionOther = nPrediction;

                return (nPrediction > dPossitiveRate);
            }

            return (false);
        }
        public double[][] MakeNeuralNetworkDataMatrix4(bool bIsTrainingMatrix, int nCandlesPerSample, int nOutputCount, int nParamsPerCandle, int nOutcomeLength, bool bStrategy)
        {
            List<NNOrder> Data = new List<NNOrder>();
            foreach (NNOrder nnoCurr in this.NeuralNetworkSelfAwarenessData)
            {
                //                if (nnoCurr.Strategy == bStrategy)
                {
                    Data.Add(nnoCurr);
                }
            }

            int nSamplesCount = Data.Count;
            int nCandlesListStart = bIsTrainingMatrix ? 0 : this.Candles.Count - nCandlesPerSample - 1;
            int nMatrixSizeToReturn = bIsTrainingMatrix ? nSamplesCount : 1;
            double[][] arrAllSampls = new double[nMatrixSizeToReturn][];
            double[] arrSample;
            bool bWMADir;
            int nWMAChangeIndex = -1;
            Candle cCurrCandle;
            Candle cCurrCandle2;
            Candle cPrevCandle;
            int nSampleIndex = 0;

            if (bIsTrainingMatrix)
            {
                foreach (NNOrder nnoCurr in Data)
                {
                    arrSample = new double[nCandlesPerSample * nParamsPerCandle + nOutputCount];
                    int nStartIndex = nCandlesPerSample < nnoCurr.CandlesHistory.Count - 2 ? 0 : (nCandlesPerSample - (nnoCurr.CandlesHistory.Count - 2)) * nParamsPerCandle;

                    bWMADir = nnoCurr.CandlesHistory[nnoCurr.CandlesHistory.Count - 1].WMADirection;
                    for (int nWMADirInex = nnoCurr.CandlesHistory.Count - 2; nWMADirInex > nnoCurr.CandlesHistory.Count - nCandlesPerSample - 1; nWMADirInex--)
                    {
                        if ((bWMADir) && !(nnoCurr.CandlesHistory[nWMADirInex].WMADirection))
                        { nWMAChangeIndex = nWMADirInex; break; }
                        bWMADir = nnoCurr.CandlesHistory[nWMADirInex].WMADirection;
                    }

                    cCurrCandle = nnoCurr.CandlesHistory[nnoCurr.CandlesHistory.Count - 1];
                    cCurrCandle2 = nnoCurr.CandlesHistory[nnoCurr.CandlesHistory.Count - 2];
                    for (int nCandleDataIndex = nStartIndex, nCandleIndex = nnoCurr.CandlesHistory.Count - 2; nCandleIndex > nnoCurr.CandlesHistory.Count - nCandlesPerSample - 2; nCandleDataIndex += nParamsPerCandle, nCandleIndex--)
                    {
                        cPrevCandle = nnoCurr.CandlesHistory[nCandleIndex];
                        arrSample[nCandleDataIndex + 0] = MathUtils.PercentChange(cCurrCandle.EndWMA, cPrevCandle.EndWMA);
                        arrSample[nCandleDataIndex + 1] = MathUtils.PercentChange((cCurrCandle.Close + cCurrCandle.Open) / 2,
                                                                                  (cPrevCandle.Close + cPrevCandle.Open) / 2);
                        arrSample[nCandleDataIndex + 2] = MathUtils.PercentChange(cPrevCandle.Low, cPrevCandle.Close);
                        arrSample[nCandleDataIndex + 3] = MathUtils.PercentChange(cPrevCandle.Low, cPrevCandle.High);
                        arrSample[nCandleDataIndex + 4] = MathUtils.PercentChange(cCurrCandle.NumOfPeiceUpdates, cPrevCandle.NumOfPeiceUpdates);

                        arrSample[nCandleDataIndex + 5] = cPrevCandle.Bid - cPrevCandle.Ask;
                        arrSample[nCandleDataIndex + 6] = cPrevCandle.Close > cPrevCandle.Open ? 1 : 0;
                        arrSample[nCandleDataIndex + 7] = cPrevCandle.WMADirection ? 1 : 0;
                        arrSample[nCandleDataIndex + 8] = cPrevCandle.EMADirection ? 1 : 0;
                        arrSample[nCandleDataIndex + 9] = nCandleIndex == nWMAChangeIndex ? 1 : 0;
                        arrSample[nCandleDataIndex + 10] = nWMAChangeIndex == -1 ? nWMAChangeIndex : nCandleIndex - nWMAChangeIndex;

                        arrSample[nCandleDataIndex + 11] = MathUtils.PercentChange(cCurrCandle.EndEMA - cCurrCandle.EndWMA,
                                                                                   cPrevCandle.EndEMA - cPrevCandle.EndWMA);
                        arrSample[nCandleDataIndex + 12] = MathUtils.PercentChange(cCurrCandle2.ExtraList[0] - cCurrCandle2.ExtraList[1],
                                                                                   cPrevCandle.ExtraList[0] - cPrevCandle.ExtraList[1]);
                        arrSample[nCandleDataIndex + 13] = MathUtils.PercentChange(cCurrCandle2.ExtraList[2] - cCurrCandle2.ExtraList[0],
                                                                                   cPrevCandle.ExtraList[2] - cPrevCandle.ExtraList[0]);
                        

                        for (int nIndicatorIndex = 3; (nIndicatorIndex < cCurrCandle.ExtraList.Count) && (nIndicatorIndex < cPrevCandle.ExtraList.Count); nIndicatorIndex++)
                        {
                            arrSample[nCandleDataIndex + 11 + nIndicatorIndex] =
                                cPrevCandle.ExtraList[nIndicatorIndex] > double.MinValue ? MathUtils.PercentChange(cCurrCandle.ExtraList[nIndicatorIndex],
                                                                                                                   cPrevCandle.ExtraList[nIndicatorIndex]) : 0;
                        }
                    }

                    arrSample[nCandlesPerSample * nParamsPerCandle + nnoCurr.ProfitIndicator] = 1;
                    arrAllSampls[nSampleIndex++] = arrSample;
                }
            }
            else
            {
                arrSample = new double[nCandlesPerSample * nParamsPerCandle + nOutputCount];

                bWMADir = this.Candles[this.Candles.Count - 1].WMADirection;
                for (int nWMADirInex = this.Candles.Count - 2; nWMADirInex > this.Candles.Count - nCandlesPerSample - 1; nWMADirInex--)
                {
                    if ((bWMADir) && !(this.Candles[nWMADirInex].WMADirection))
                    { nWMAChangeIndex = nWMADirInex; break; }
                    bWMADir = this.Candles[nWMADirInex].WMADirection;
                }

                cCurrCandle = this.Candles[this.Candles.Count - 2];
                for (int nCandleDataIndex = 0, nCandleIndex = this.Candles.Count - 3; nCandleIndex > nCandlesListStart - 2; nCandleDataIndex += nParamsPerCandle, nCandleIndex--)
                {
                    cPrevCandle = this.Candles[nCandleIndex];
                    arrSample[nCandleDataIndex + 0] = MathUtils.PercentChange(cCurrCandle.EndWMA, cPrevCandle.EndWMA);
                    arrSample[nCandleDataIndex + 1] = MathUtils.PercentChange((cCurrCandle.Close + cCurrCandle.Open) / 2,
                                                                              (cPrevCandle.Close + cPrevCandle.Open) / 2);
                    arrSample[nCandleDataIndex + 2] = MathUtils.PercentChange(cPrevCandle.Low, cPrevCandle.Close);
                    arrSample[nCandleDataIndex + 3] = MathUtils.PercentChange(cPrevCandle.Low, cPrevCandle.High);
                    arrSample[nCandleDataIndex + 4] = MathUtils.PercentChange(cCurrCandle.NumOfPeiceUpdates, cPrevCandle.NumOfPeiceUpdates);

                    arrSample[nCandleDataIndex + 5] = cPrevCandle.Bid - cPrevCandle.Ask;
                    arrSample[nCandleDataIndex + 6] = cPrevCandle.Close > cPrevCandle.Open ? 1 : 0;
                    arrSample[nCandleDataIndex + 7] = cPrevCandle.WMADirection ? 1 : 0;
                    arrSample[nCandleDataIndex + 8] = cPrevCandle.EMADirection ? 1 : 0;
                    arrSample[nCandleDataIndex + 9] = nCandleIndex == nWMAChangeIndex ? 1 : 0;
                    arrSample[nCandleDataIndex + 10] = nWMAChangeIndex == -1 ? nWMAChangeIndex : nCandleIndex - nWMAChangeIndex;

                    arrSample[nCandleDataIndex + 11] = MathUtils.PercentChange(cCurrCandle.EndEMA - cCurrCandle.EndWMA,
                                                                               cPrevCandle.EndEMA - cPrevCandle.EndWMA);               
                    arrSample[nCandleDataIndex + 12] = MathUtils.PercentChange(cCurrCandle.ExtraList[0] - cCurrCandle.ExtraList[1],
                                                                               cPrevCandle.ExtraList[0] - cPrevCandle.ExtraList[1]);
                    arrSample[nCandleDataIndex + 13] = MathUtils.PercentChange(cCurrCandle.ExtraList[2] - cCurrCandle.ExtraList[0],
                                                                               cPrevCandle.ExtraList[2] - cPrevCandle.ExtraList[0]);
                    

                    for (int nIndicatorIndex = 3; (nIndicatorIndex < cCurrCandle.ExtraList.Count) && (nIndicatorIndex < cPrevCandle.ExtraList.Count); nIndicatorIndex++)
                    {
                        arrSample[nCandleDataIndex + 11 + nIndicatorIndex] =
                            cPrevCandle.ExtraList[nIndicatorIndex] > double.MinValue ? MathUtils.PercentChange(cCurrCandle.ExtraList[nIndicatorIndex],
                                                                                                               cPrevCandle.ExtraList[nIndicatorIndex]) : 0;
                    }
                }

                arrAllSampls[nSampleIndex++] = arrSample;
            }

            return (arrAllSampls);
        }
    }
}
