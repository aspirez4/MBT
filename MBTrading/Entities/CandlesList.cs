using System;
using System.Collections.Generic;
using System.Linq;
using MBTrading.Utils;
using System.IO;
using MBTrading.Entities;

namespace MBTrading
{
    public class CandlesList
    {
        public List<Candle>  NeuralNetworkRawData = new List<Candle>();
        public List<NNOrder> NeuralNetworkSelfAwarenessData =  new List<NNOrder>();
        public List<NNOrder> NeuralNetworkSelfAwarenessCollection = new List<NNOrder>();
        
        public NeuralNetwork NNStrategy;
        public NeuralNetwork NNOther;


        public Share        ParentShare;
        public List<Candle> Candles;
        public int          MinuteCandles;
        public bool         Primary;
        public Candle       LastCandle;
        public double       CurrPrice;
        public Candle       PrevCandle;
        public int          Count;
        public int          CountDec;
        public double       LowestPrice;
        public double       HighestPrice; 
        
        // Previous indicators refers to the last bit of the previous candle

        // RSI
        public  double RSI;
        public  double RS;
        private double AVGGain;
        private double AVGLoss;
        private double LowestRSI;
        private double HighestRSI;
        private int    RSIParameters_LengthDec;
        // Stochastic
        public  double Stochastic;
        public  double PrevStochastic;
        private int    StochasticParameterKsmoothingLengthDec;
        private int    StochasticParameterDsmoothingLengthDec;
        public  List<double> Stochastic_K_SmoothingList;
        public  List<double> Stochastic_D_SmoothingList;
        // StochasticRSI
        public double  StochasticRSI;
        public double  PrevStochasticRSI;
        public double  AVGStochasticRSI_K;
        public double  AVGStochasticRSI_D;
        public double  PrevAVGStochasticRSI_K;
        public double  PrevAVGStochasticRSI_D;
        public List<double> RSIList;
        // SMA
        public double  SMA;
        public double  PrevSMA;
        public double  SMA_UpperEnvelope;
        public double  SMA_LowerEnvelope;
        public double  SMA_UpperBollinger;
        public double  SMA_LowerBollinger;
        public List<Candle> SMAList;
        // EMA
        public  double EMA;
        public  double PrevEMA;
        public  bool   EMADirection;
        public  bool   PrevEMADirection;
        private double EMAMultiplier;
        public  double Derivative;
        public  double PrevDerivative;
        public  bool   DerivativeDierection;
        public  double MaxEMA;
        public  double MinEMA;
        public  List<double> EMAList;
        // WMA
        public double  WMA;
        public double  PrevWMA;
        public bool    WMADirection;
        public bool    PrevWMADirection;
        private int    WMAParametersDenominator;
        // Awesome Oscillator 
        public double  Awesome;
        public double  PrevAwesome;
        public List<Candle> AwesomeMidSMAShortList;
        public List<Candle> AwesomeMidSMALongList;
        public double  CandleSizeAVG;
        // TDI
        public double PrevTDI_Green;
        public double PrevTDI_Red;
        public double TDI_Green;
        public double TDI_Red;
        public double TDI_Upper;
        public double TDI_Lower;
        public double TDI_Mid;
        public List<double> TDIRSIList;





        public int P, N = 0;
        int nPrecentageToTrain      = 75;
        int nCandlesPerSample = 10; //48;
        int nParamsPerCandle        = 18;
        int nOutcomeIntervalLength  = 36;
        int numOutput               = 2;
        int maxEpochsLoop           = 10;       // 2000
        double learnRate            = 0.05;     // 0.05
        double momentum             = 0.01;     // 0.01
        double weightDecay          = 0.0001;   // 0.0001
        double meanSquaredError     = 0.001;    // 0.020









        public CandlesList(List<Candle> lstInitializeList,
                           Share        sParentShare,
                           bool         bPrimary)
        {
            this.RSIParameters_LengthDec        = Consts.RSI_PARAMETERS_LENGTH - 1;
            this.ParentShare                    = sParentShare;
            this.Candles                        = lstInitializeList;
            this.RSIList                        = new List<double>();
            this.TDIRSIList                     = new List<double>();
            this.Stochastic_K_SmoothingList     = new List<double>();
            this.Stochastic_D_SmoothingList     = new List<double>();
            this.SMAList                        = new List<Candle>();
            this.EMAList                        = new List<double>();
            this.AwesomeMidSMAShortList         = new List<Candle>();
            this.AwesomeMidSMALongList          = new List<Candle>();

            this.Primary                        = bPrimary;
            this.MinuteCandles                  = this.Primary ? Consts.MINUTE_CANDLES_PRIMARY : Consts.MINUTE_CANDLES_SECONDARY;
            this.Count                          = lstInitializeList.Count;
            this.CountDec                       = this.Count - 1;
            this.LastCandle                     = this.Candles[this.CountDec];
            this.PrevCandle                     = this.Candles[this.CountDec - 1];

            this.LowestPrice                    = double.MaxValue;
            this.HighestPrice                   = double.MinValue;
            this.LowestRSI                      = double.MaxValue;
            this.HighestRSI                     = double.MinValue;

            #region RS Parameters
            double dChangingRate;
            Candle cCurrCandle;
            for (int nStochasticIndex = this.CountDec; nStochasticIndex > this.CountDec - Consts.STOCHASTIC_PARAMETERS_LENGTH; nStochasticIndex--)
            {
                cCurrCandle = this.Candles[nStochasticIndex];
                if (cCurrCandle.High > this.HighestPrice)
                {
                    this.HighestPrice = cCurrCandle.High;
                }

                if (cCurrCandle.Low < this.LowestPrice)
                {
                    this.LowestPrice = cCurrCandle.Low;
                }
            }
            for (int nRSIIndex = this.CountDec; nRSIIndex > this.CountDec - Consts.RSI_PARAMETERS_LENGTH; nRSIIndex--)
            {
                cCurrCandle = this.Candles[nRSIIndex];
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

            this.AVGGain                        /= Consts.RSI_PARAMETERS_LENGTH;
            this.AVGLoss                        /= Consts.RSI_PARAMETERS_LENGTH;
            this.RS                             = this.AVGGain / this.AVGLoss;
            this.RSI                            = 100 - (100 / (1 + this.RS));
            this.StochasticRSI                  = 50;
            this.PrevStochasticRSI              = 50;

            this.Stochastic                     = 100 * ((this.Candles[this.CountDec].Close - this.LowestPrice) / (this.HighestPrice - this.LowestPrice));
            this.PrevStochastic                 = this.Stochastic;
            this.AVGStochasticRSI_K             = this.Stochastic;
            this.AVGStochasticRSI_D             = this.Stochastic;
            this.PrevAVGStochasticRSI_K         = this.AVGStochasticRSI_K;
            this.PrevAVGStochasticRSI_D         = this.AVGStochasticRSI_D;

            this.EMAMultiplier = (2 / (double)(Consts.esMA_PARAMETERS_LENGTH + 1));
            this.StochasticParameterKsmoothingLengthDec = Consts.STOCHASTIC_PARAMETERS_K_SMOOTHING_LENGTH - 1;
            this.StochasticParameterDsmoothingLengthDec = Consts.STOCHASTIC_PARAMETERS_D_SMOOTHING_LENGTH - 1;

            for (int nCounter = 0; nCounter < Consts.STOCHASTIC_PARAMETERS_LENGTH; nCounter++)                      { this.RSIList.Add(this.RSI); }
            for (int nCounter = 0; nCounter < 34; nCounter++)                                                       { this.TDIRSIList.Add(this.RSI); }
            for (int nCounter = 0; nCounter < Consts.STOCHASTIC_PARAMETERS_K_SMOOTHING_LENGTH; nCounter++)          { this.Stochastic_K_SmoothingList.Add(this.StochasticRSI); }
            for (int nCounter = 0; nCounter < Consts.STOCHASTIC_PARAMETERS_D_SMOOTHING_LENGTH; nCounter++)          { this.Stochastic_D_SmoothingList.Add(this.StochasticRSI); }
            for (int nCounter = 1; nCounter <= Consts.WMA_PARAMETERS_LENGTH; nCounter++)                            { this.WMAParametersDenominator += nCounter; }
            int nStopIndex = Math.Max(0, this.CountDec - Consts.esMA_PARAMETERS_LENGTH);
            for (int nCounter = this.CountDec; nCounter > nStopIndex; nCounter--)                                   { this.SMAList.Add(this.Candles[nCounter]); this.EMAList.Add(this.Candles[nCounter].Close); }
            for (int nCounter = 0; nCounter < 34; nCounter++)                                                       { this.AwesomeMidSMAShortList.Add(this.Candles[this.CountDec]); }
            for (int nCounter = 0; nCounter < 5; nCounter++)                                                        { this.AwesomeMidSMALongList.Add(this.Candles[this.CountDec]); }


            this.NewWMA();
            this.NewSMABand();
            this.NewAwesome();
            this.PrevAwesome = this.Awesome;
            this.EMA         = this.SMA;
            this.PrevEMA     = this.SMA;
            this.PrevSMA     = this.SMA;
            this.PrevWMA     = this.WMA;
            this.MaxEMA      = this.EMAList.Max();
            this.MinEMA      = this.EMAList.Min();

            this.Derivative = this.EMA / this.PrevEMA;
            this.PrevDerivative = this.Derivative;

            this.DerivativeDierection = false;
            this.EMADirection = true;
            this.PrevEMADirection = true;
            this.WMADirection = true;
            this.PrevWMADirection = true;
        }

        public bool AddOrUpdatePrice(MarketData mdCurrMarketData)
        {
            // Updating the HighestPrice and LowestPrice to be The LastCandle's prices if justified
            if (mdCurrMarketData.Value > this.HighestPrice) { this.HighestPrice = mdCurrMarketData.Value; }
            if (mdCurrMarketData.Value < this.LowestPrice)  { this.LowestPrice = mdCurrMarketData.Value; }


            // Get the relevant candle of the share. -> If found, update the candle
            if (this.LastCandle.StartDate.Compare(mdCurrMarketData.Time, this.Primary))
            {
                this.LastCandle.UpdateCandle(mdCurrMarketData);
                this.CurrPrice = this.LastCandle.Bid;

                this.UpdateRSI();
                this.UpdateStochasticRSI();
                this.UpdateStochastic();
                this.UpdateSMABand();
                this.UpdateEMA();
                this.UpdateWMA();
                this.UpdateAwesome();
                this.UpdateTDI();

                // Return - "NotNew" indication
                return (false);
            }
            // Else - it's new candle - Add it to share
//            else if (mdCurrMarketData.Price != -1)
            else if (mdCurrMarketData.DataType == MarketDataType.Ask)
            {
                this.LastCandle.WMADirection = this.WMADirection;
                this.LastCandle.EMADirection = this.EMADirection;
                this.LastCandle.EndWMA = this.WMA;
                this.LastCandle.EndEMA = this.EMA;
                this.LastCandle.EndTDI_Green = this.TDI_Green;
                this.LastCandle.EndTDI_Red = this.TDI_Red;
                this.LastCandle.EndTDI_Mid = this.TDI_Mid;
                this.LastCandle.ExtraList.Add(this.SMA); 
                this.LastCandle.ExtraList.Add(this.SMA_LowerBollinger);
                this.LastCandle.ExtraList.Add(this.SMA_UpperBollinger);
                this.LastCandle.ExtraList.Add(this.RSI);
                this.LastCandle.ExtraList.Add(this.Stochastic);
                this.LastCandle.ExtraList.Add(this.StochasticRSI);
                this.LastCandle.ExtraList.Add(this.Awesome);
                this.LastCandle.ExtraList.Add(this.TDI_Green);
                this.LastCandle.ExtraList.Add(this.TDI_Red);
                this.LastCandle.ExtraList.Add(this.TDI_Upper);
                this.LastCandle.ExtraList.Add(this.TDI_Lower);

                this.LastCandle.LogCandle(this.ParentShare, this.SMA_LowerBollinger); 

                if (this.NeuralNetworkRawData.Count == Consts.NEURAL_NETWORK_NUM_OF_TRAINING_CANDLES) { this.NeuralNetworkRawData.RemoveAt(0); }
                this.NeuralNetworkRawData.Add(this.LastCandle);

                MongoDBUtils.DBEventAfterCandleFinished(this.ParentShare, this.LastCandle);

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

                this.Candles.RemoveAt(0);
                Candle cFirstCandle = this.Candles[0];
                if (cFirstCandle.High == this.HighestPrice) { this.HighestPrice = this.Candles.Max(C => C.High); }
                if (cFirstCandle.Low == this.LowestPrice) { this.LowestPrice = this.Candles.Min(C => C.Low); }
                this.Candles.Add(cCurrCandle);
                this.PrevCandle = this.LastCandle;
                this.LastCandle = cCurrCandle;
                this.CurrPrice = this.LastCandle.Bid;
                
                this.SMAList.RemoveAt(0);
                this.AwesomeMidSMALongList.RemoveAt(0);
                this.AwesomeMidSMAShortList.RemoveAt(0);
                this.SMAList.Add(cCurrCandle);
                this.AwesomeMidSMALongList.Add(cCurrCandle);
                this.AwesomeMidSMAShortList.Add(cCurrCandle);

                this.NewRSI();
                this.NewStochasticRSI();
                this.NewStochastic();
                this.NewSMABand();
                this.NewEMA();
                this.NewWMA();
                this.NewAwesome();
                this.NewTDI();

                this.LastCandle.StartWMA = this.WMA;
                this.LastCandle.StartEMA = this.EMA;
                this.LastCandle.StartTDI_Green  = this.TDI_Green;
                this.LastCandle.StartTDI_Red    = this.TDI_Red;

                // Return - "New" indication
                return (true);
            }
            else
            {
                return (false);
            }
        }
        //public void NeuralNetworkActivate()
        //{
        //    int numInput = nCandlesPerSample * nParamsPerCandle;
        //    int numHidden = numInput * 2;

        //    double[][] AllData;
        //    double[][] TrainData;
        //    double[][] TestData;
        //    double[][] NormalizedTraingData;
        //    double[][] NormalizedTestData;

        //    // Initialize and Train the NeuralNetwork
        //    NeuralNetwork nn = new NeuralNetwork(numInput, numHidden, numOutput);
        //    AllData = this.MakeNeuralNetworkDataMatrix(true, nCandlesPerSample, numOutput, nParamsPerCandle, nOutcomeIntervalLength);
        //    NeuralNetwork.MakeTrainAndTestRandom(AllData, out TrainData, out TestData, nPrecentageToTrain);
            
        //    // Normalizing Data
        //    NormalizedTraingData = NeuralNetwork.NormalizeMatrix(TrainData, TrainData[0].Length - numOutput);
        //    NormalizedTestData   = NeuralNetwork.NormalizeMatrix(TestData, TestData[0].Length - numOutput);

        //    // Save matrixes
        //    nn.RawTrainData = TrainData;
        //    nn.NormalizedTestData = NormalizedTestData;

        //    // Train NeuralNetwork
        //    nn.InitializeWeights();
        //    nn.Train(NormalizedTraingData, maxEpochsLoop, learnRate, momentum, weightDecay, meanSquaredError);

        //    // Accuracy check
        //    nn.AccuracyRate = nn.Accuracy(NormalizedTestData);

        //    // Set the new NeuralNetwork as the Current
        //    this.NNStrategy = nn;
        //}
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


            //// Initialize and Train the NeuralNetwork
            //nn = new NeuralNetwork(numInput, numHidden, numOutput);
            //AllData = this.MakeNeuralNetworkDataMatrix2(true, nCandlesPerSample, numOutput, nParamsPerCandle, nOutcomeIntervalLength, false);
            //NeuralNetwork.MakeTrainAndTestRandom(AllData, out TrainData, out TestData, nPrecentageToTrain);

            //// Normalizing Data
            //NormalizedTraingData = NeuralNetwork.NormalizeMatrix(TrainData, TrainData[0].Length - numOutput);
            //NormalizedTestData = NeuralNetwork.NormalizeMatrix(TestData, TestData[0].Length - numOutput);

            //// Save matrixes
            //nn.RawTrainData = TrainData;
            //nn.NormalizedTestData = NormalizedTestData;

            //// Train NeuralNetwork
            //nn.InitializeWeights();
            //nn.Train(NormalizedTraingData, maxEpochsLoop, learnRate, momentum, weightDecay, meanSquaredError);

            //// Accuracy check
            //nn.AccuracyRate = nn.Accuracy(NormalizedTestData);

            //// Set the new NeuralNetwork as the Current
            //this.NNOther = nn;
        }
        //public bool NeuralNetworkPredict(double dPossitiveRate)
        //{
        //    if ((this.NNStrategy != null) && (this.NNStrategy.AccuracyRate > 0.6))
        //    {
        //        double[][] arrPredictSet = MakeNeuralNetworkDataMatrix(false, nCandlesPerSample,numOutput, nParamsPerCandle, nOutcomeIntervalLength);
        //        this.NNStrategy.RawTrainData[this.NNStrategy.RawTrainData.Length - 1] = arrPredictSet[0];
        //        double[][] arrNormalizePredictionMatrix = NeuralNetwork.NormalizeMatrix(this.NNStrategy.RawTrainData, this.NNStrategy.RawTrainData[0].Length - numOutput);
        //        double nPrediction = this.NNStrategy.Predict(arrNormalizePredictionMatrix[arrNormalizePredictionMatrix.Length - 1]);

        //        this.Candles[this.CountDec - 1].ProfitPredictionStrategy = nPrediction;


        //        return (nPrediction > dPossitiveRate);
        //    }
        //    return (false);
        //}
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
        //public double[][] MakeNeuralNetworkDataMatrix(bool bIsTrainingMatrix, int nCandlesPerSample, int nOutputCount, int nParamsPerCandle, int nOutcomeLength)
//        {    
//            int nSamplesCount = this.NeuralNetworkRawData.Count;
//            int nCandlesListStart = bIsTrainingMatrix ? 0 : nSamplesCount - 1 - nCandlesPerSample;
//            int nMatrixSizeToReturn = bIsTrainingMatrix ? nSamplesCount - nCandlesPerSample - nOutcomeLength : 1;
//            double[][] arrAllSampls = new double[nMatrixSizeToReturn][];
//            double[] arrSample;
//            Candle cCurrCandle;
//            Candle cPrevCandle;
//            Candle cOutcomeCandle;

//            for (int nSampleIndex = 0; nSampleIndex < arrAllSampls.Length; nSampleIndex++)
//            {
//                arrSample = new double[nCandlesPerSample * nParamsPerCandle + nOutputCount];

//                for (int nCandleDataIndex = nParamsPerCandle; nCandleDataIndex < ((nCandlesPerSample + 1) * nParamsPerCandle); nCandleDataIndex += nParamsPerCandle)
//                {
//                    cCurrCandle = this.NeuralNetworkRawData[nCandlesListStart + nSampleIndex + (nCandleDataIndex / nParamsPerCandle)];
//                    cPrevCandle = this.NeuralNetworkRawData[nCandlesListStart + nSampleIndex + (nCandleDataIndex / nParamsPerCandle) - 1];
//                    arrSample[nCandleDataIndex - 9] = cCurrCandle.Close > cCurrCandle.Open ? 1 : 0;
//                    arrSample[nCandleDataIndex - 8] = cCurrCandle.Close > cCurrCandle.Open ? cCurrCandle.Open - cCurrCandle.Low : cCurrCandle.Close - cCurrCandle.Low;
//                    arrSample[nCandleDataIndex - 7] = cCurrCandle.Low - cCurrCandle.Extra;
//                    arrSample[nCandleDataIndex - 6] = cCurrCandle.NumOfPeiceUpdates;
//                    arrSample[nCandleDataIndex - 5] = cCurrCandle.EndWMA - cCurrCandle.EndEMA;
//                    arrSample[nCandleDataIndex - 4] = cCurrCandle.WMADirection ? 1 : 0;
//                    arrSample[nCandleDataIndex - 3] = cCurrCandle.CandleVolume;
//                    arrSample[nCandleDataIndex - 2] = cCurrCandle.Close - cCurrCandle.Open;
//                    arrSample[nCandleDataIndex - 1] = (cCurrCandle.PricesSum / cCurrCandle.NumOfPeiceUpdates) -
//                                                      (cPrevCandle.PricesSum / cPrevCandle.NumOfPeiceUpdates);
//                }

//                if (bIsTrainingMatrix)
//                {
//                    cCurrCandle = this.NeuralNetworkRawData[nCandlesListStart + nSampleIndex + nCandlesPerSample];
//                    cOutcomeCandle = this.NeuralNetworkRawData[nCandlesListStart + nSampleIndex + nCandlesPerSample + nOutcomeLength];
//                    double dSampleProfit = ((cOutcomeCandle.Close + cOutcomeCandle.Open) / 2) - (cCurrCandle.Close);

//                    dSampleProfit /= this.ParentShare.PipsUnit;

//                    if (dSampleProfit <= 50)
//                    {
//                        arrSample[nCandlesPerSample * nParamsPerCandle] = 1;
//                    }
//                    else if (dSampleProfit > 50)
//                    {
//                        arrSample[nCandlesPerSample * nParamsPerCandle + 1] = 1;
//                    }
//                    //else if ((0 <= dSampleProfit) && (dSampleProfit < Consts.NEURAL_NETWORK_PROFIT_OR_LOSS_PIPS_RANGE))
//                    //{
//                    //    arrSample[nCandlesPerSample * nParamsPerCandle + 2 - 2] = 1;
//                    //}
//                    //else if (Consts.NEURAL_NETWORK_PROFIT_OR_LOSS_PIPS_RANGE <= dSampleProfit)
//                    //{
//                    //    arrSample[nCandlesPerSample * nParamsPerCandle + 3 - 2] = 1;
//                    //}
//                }

//                arrAllSampls[nSampleIndex] = arrSample;
//            }

//            return (arrAllSampls);
//        }
//        public double[][] MakeNeuralNetworkDataMatrix1(bool bIsTrainingMatrix, int nCandlesPerSample, int nOutputCount, int nParamsPerCandle, int nOutcomeLength)
//        {
//            int nSamplesCount = this.NeuralNetworkRawData.Count;
//            int nCandlesListStart = bIsTrainingMatrix ? 0 : nSamplesCount - 1 - nCandlesPerSample;
//            int nMatrixSizeToReturn = bIsTrainingMatrix ? nSamplesCount - nCandlesPerSample - nOutcomeLength : 1;
//            double[][] arrAllSampls = new double[nMatrixSizeToReturn][];
//            double[] arrSample;
//            Candle cCurrCandle;
//            Candle cPrevCandle;
//            Candle cOutcomeCandle;
//            double temp;

//            for (int nSampleIndex = 0; nSampleIndex < arrAllSampls.Length; nSampleIndex++)
//            {
//                arrSample = new double[nParamsPerCandle + nOutputCount];

//                arrSample[12] = 5;
//                arrSample[9] = 5;
//                arrSample[6] = 5;
//                arrSample[5] = 5;
//                arrSample[2] = 5;
//                arrSample[0] = 5;

//                for (int nCandleDataIndex = nSampleIndex + nCandlesPerSample + 1; nCandleDataIndex > nSampleIndex; nCandleDataIndex--)
//                {
//                    cCurrCandle = this.NeuralNetworkRawData[nCandleDataIndex];
//                    cPrevCandle = this.NeuralNetworkRawData[nCandleDataIndex - 1];

//                    temp = cCurrCandle.Close > cCurrCandle.Open ? 1 : 0;
//                    arrSample[12] = arrSample[12] * 10 + temp;
                    
//                    temp = cCurrCandle.Close > cCurrCandle.Open ? cCurrCandle.Open - cCurrCandle.Low : cCurrCandle.Close - cCurrCandle.Low;
//                    arrSample[11] = arrSample[11] * 10 + temp;
                    
//                    arrSample[10] = arrSample[10] * 10 + Math.Abs(cCurrCandle.Low - cCurrCandle.Extra);
//                    temp = (cCurrCandle.Low - cCurrCandle.Extra) < 0 ? 0 : 1;
//                    arrSample[9] = arrSample[9] * 10 + temp;
                    
//                    arrSample[8] = arrSample[8] * 10 + cCurrCandle.NumOfPeiceUpdates;
                    
//                    arrSample[7] = arrSample[7] * 10 + Math.Abs(cCurrCandle.EndWMA - cCurrCandle.EndEMA);
//                    temp = cCurrCandle.EndWMA - cCurrCandle.EndEMA < 0 ? 0 : 1;
//                    arrSample[6] = arrSample[6] * 10 + temp;
                    
//                    temp = (cCurrCandle.WMADirection ? 1 : 0);
//                    arrSample[5] = arrSample[5] * 10 + temp;
                    
//                    arrSample[4] = arrSample[4] * 10 + cCurrCandle.CandleVolume;
                    
//                    arrSample[3] = arrSample[3] * 10 + Math.Abs(cCurrCandle.Close - cCurrCandle.Open);
//                    temp = cCurrCandle.Close - cCurrCandle.Open < 0 ? 0 : 1;
//                    arrSample[2] = arrSample[2] * 10 + temp;
                    
//                    arrSample[1] = arrSample[1] * 10 + Math.Abs((cCurrCandle.PricesSum / cCurrCandle.NumOfPeiceUpdates) - (cPrevCandle.PricesSum / cPrevCandle.NumOfPeiceUpdates));
//                    temp = (cCurrCandle.PricesSum / cCurrCandle.NumOfPeiceUpdates) - (cPrevCandle.PricesSum / cPrevCandle.NumOfPeiceUpdates) < 0 ? 0 : 1;
//                    arrSample[0] = arrSample[0] * 10 + temp;
//                }

//                if (bIsTrainingMatrix)
//                {
//                    cCurrCandle = this.NeuralNetworkRawData[nCandlesListStart + nSampleIndex + nCandlesPerSample];
//                    cOutcomeCandle = this.NeuralNetworkRawData[nCandlesListStart + nSampleIndex + nCandlesPerSample + nOutcomeLength];
//                    double dSampleProfit = (cOutcomeCandle.PricesSum / cOutcomeCandle.NumOfPeiceUpdates) -
//                                           (cCurrCandle.Close);
//                    dSampleProfit /= this.ParentShare.PipsUnit;

//                    if (dSampleProfit < -20)
//                    {
//                        arrSample[nParamsPerCandle] = 1;
//                    }
//                    else if ((-20 <= dSampleProfit) && (dSampleProfit < 0))
//                    {
//                        arrSample[nParamsPerCandle + 1 - 1] = 1;
//                    }
//                    else if ((0 <= dSampleProfit) && (dSampleProfit < 20))
//                    {
//                        arrSample[nParamsPerCandle + 2 - 2] = 1;
//                    }
//                    else if (20 <= dSampleProfit)
//                    {
//                        arrSample[nParamsPerCandle + 3 - 2] = 1;
//                    }
//                }

//                arrAllSampls[nSampleIndex] = arrSample;
//            }

//            return (arrAllSampls);
//        }
//        public double[][] MakeNeuralNetworkDataMatrix2(bool bIsTrainingMatrix, int nCandlesPerSample, int nOutputCount, int nParamsPerCandle, int nOutcomeLength, bool bStrategy)
//        {
//            List<NNOrder> Data = new List<NNOrder>();
//            foreach (NNOrder nnoCurr in this.NeuralNetworkSelfAwarenessData)
//            {
////                if (nnoCurr.Strategy == bStrategy)
//                {
//                    Data.Add(nnoCurr);
//                }
//            }

//            int nSamplesCount = Data.Count;
//            int nCandlesListStart = bIsTrainingMatrix ? 0 : this.Candles.Count - nCandlesPerSample - 1;
//            int nMatrixSizeToReturn = bIsTrainingMatrix ? nSamplesCount : 1;
//            double[][] arrAllSampls = new double[nMatrixSizeToReturn][];
//            double[] arrSample;
//            Candle cCurrCandle;
//            Candle cPrevCandle;
//            int nSampleIndex = 0;

//            if (bIsTrainingMatrix)
//            {
//                foreach (NNOrder nnoCurr in Data)
//                {
//                    arrSample = new double[nCandlesPerSample * nParamsPerCandle + nOutputCount];
//                    int nStartIndex = (nCandlesPerSample - (nnoCurr.CandlesHistory.Count - 2)) * nParamsPerCandle;


//                    for (int nCandleDataIndex = nStartIndex, nCandleIndex = nnoCurr.CandlesHistory.Count - nCandlesPerSample; nCandleIndex < nnoCurr.CandlesHistory.Count; nCandleDataIndex += nParamsPerCandle, nCandleIndex++)
//                    {
//                        cCurrCandle = nnoCurr.CandlesHistory[nCandleIndex];
//                        cPrevCandle = nnoCurr.CandlesHistory[nCandleIndex - 1];
//                        arrSample[nCandleDataIndex + 0] = cCurrCandle.Close > cCurrCandle.Open ? 1 : 0;
//                        arrSample[nCandleDataIndex + 1] = cCurrCandle.Close > cCurrCandle.Open ? cCurrCandle.Open - cCurrCandle.Low : cCurrCandle.Close - cCurrCandle.Low;
//                        arrSample[nCandleDataIndex + 2] = (cCurrCandle.Extra > double.MinValue) ? (cCurrCandle.Low - cCurrCandle.Extra) : 0;
//                        arrSample[nCandleDataIndex + 3] = cCurrCandle.NumOfPeiceUpdates;
//                        arrSample[nCandleDataIndex + 4] = cCurrCandle.EndWMA - cCurrCandle.EndEMA;
//                        arrSample[nCandleDataIndex + 5] = cCurrCandle.WMADirection ? 1 : 0;
//                        arrSample[nCandleDataIndex + 6] = cCurrCandle.CandleVolume;
//                        arrSample[nCandleDataIndex + 7] = cCurrCandle.Close - cCurrCandle.Open;
//                        arrSample[nCandleDataIndex + 8] = (cCurrCandle.PricesSum / cCurrCandle.NumOfPeiceUpdates) -
//                                                          (cPrevCandle.PricesSum / cPrevCandle.NumOfPeiceUpdates);
//                    }

//                    arrSample[nCandlesPerSample * nParamsPerCandle + nnoCurr.ProfitIndicator] = 1;
//                    arrAllSampls[nSampleIndex++] = arrSample;
//                }
//            }
//            else
//            {
//                arrSample = new double[nCandlesPerSample * nParamsPerCandle + nOutputCount];

//                for (int nCandleDataIndex = 0, nCandleIndex = nCandlesListStart; nCandleIndex < this.Candles.Count - 1; nCandleDataIndex += nParamsPerCandle, nCandleIndex++)
//                {
//                    cCurrCandle = this.Candles[nCandleIndex];
//                    cPrevCandle = this.Candles[nCandleIndex - 1];
//                    arrSample[nCandleDataIndex + 0] = cCurrCandle.Close > cCurrCandle.Open ? 1 : 0;
//                    arrSample[nCandleDataIndex + 1] = cCurrCandle.Close > cCurrCandle.Open ? cCurrCandle.Open - cCurrCandle.Low : cCurrCandle.Close - cCurrCandle.Low;
//                    arrSample[nCandleDataIndex + 2] = (cCurrCandle.Extra > double.MinValue) ? (cCurrCandle.Low - cCurrCandle.Extra) : 0;
//                    arrSample[nCandleDataIndex + 3] = cCurrCandle.NumOfPeiceUpdates;
//                    arrSample[nCandleDataIndex + 4] = cCurrCandle.EndWMA - cCurrCandle.EndEMA;
//                    arrSample[nCandleDataIndex + 5] = cCurrCandle.WMADirection ? 1 : 0;
//                    arrSample[nCandleDataIndex + 6] = cCurrCandle.CandleVolume;
//                    arrSample[nCandleDataIndex + 7] = cCurrCandle.Close - cCurrCandle.Open;
//                    arrSample[nCandleDataIndex + 8] = (cCurrCandle.PricesSum / cCurrCandle.NumOfPeiceUpdates) -
//                                                      (cPrevCandle.PricesSum / cPrevCandle.NumOfPeiceUpdates);
//                }
                
//                arrAllSampls[nSampleIndex++] = arrSample;
//            }

//            return (arrAllSampls);
//        }
//        public double[][] MakeNeuralNetworkDataMatrix3(bool bIsTrainingMatrix, int nCandlesPerSample, int nOutputCount, int nParamsPerCandle, int nOutcomeLength, bool bStrategy)
//        {
//            List<NNOrder> Data = new List<NNOrder>();
//            foreach (NNOrder nnoCurr in this.NeuralNetworkSelfAwarenessData)
//            {
////                if (nnoCurr.Strategy == bStrategy)
//                {
//                    Data.Add(nnoCurr);
//                }
//            }

//            int nSamplesCount = Data.Count;
//            int nCandlesListStart = bIsTrainingMatrix ? 0 : this.Candles.Count - nCandlesPerSample - 1;
//            int nMatrixSizeToReturn = bIsTrainingMatrix ? nSamplesCount : 1;
//            double[][] arrAllSampls = new double[nMatrixSizeToReturn][];
//            double[] arrSample;
//            Candle cCurrCandle;
//            Candle cPrevCandle;
//            int nSampleIndex = 0;

//            if (bIsTrainingMatrix)
//            {
//                foreach (NNOrder nnoCurr in Data)
//                {
//                    arrSample = new double[nCandlesPerSample * nParamsPerCandle + nOutputCount];
//                    int nStartIndex = (nCandlesPerSample - (nnoCurr.CandlesHistory.Count - 2)) * nParamsPerCandle;

//                    for (int nCandleDataIndex = nStartIndex, nCandleIndex = nnoCurr.CandlesHistory.Count - 2; nCandleIndex > nnoCurr.CandlesHistory.Count - nCandlesPerSample - 2; nCandleDataIndex += nParamsPerCandle, nCandleIndex--)
//                    {
//                        cCurrCandle = nnoCurr.CandlesHistory[nCandleIndex];
//                        arrSample[nCandleDataIndex + 0] = cCurrCandle.EndWMA;
//                        arrSample[nCandleDataIndex + 1] = cCurrCandle.EndEMA;
//                        arrSample[nCandleDataIndex + 2] = cCurrCandle.WMADirection ? 1 : 0;
//                        arrSample[nCandleDataIndex + 3] = cCurrCandle.EMADirection ? 1 : 0;
//                        arrSample[nCandleDataIndex + 4] = cCurrCandle.Close - cCurrCandle.Open;
//                        arrSample[nCandleDataIndex + 5] = cCurrCandle.High - cCurrCandle.Low;
//                        arrSample[nCandleDataIndex + 6] = cCurrCandle.Close > cCurrCandle.Open ? cCurrCandle.Open - cCurrCandle.Low : cCurrCandle.Close - cCurrCandle.Low;
//                        arrSample[nCandleDataIndex + 7] = (cCurrCandle.Extra > double.MinValue) ? (cCurrCandle.Low - cCurrCandle.Extra) : 0;
//                        arrSample[nCandleDataIndex + 8] = cCurrCandle.NumOfPeiceUpdates;
//                        arrSample[nCandleDataIndex + 9] = cCurrCandle.EndWMA - cCurrCandle.EndEMA;
//                    }

//                    arrSample[nCandlesPerSample * nParamsPerCandle + nnoCurr.ProfitIndicator] = 1;
//                    arrAllSampls[nSampleIndex++] = arrSample;
//                }
//            }
//            else
//            {
//                arrSample = new double[nCandlesPerSample * nParamsPerCandle + nOutputCount];

//                for (int nCandleDataIndex = 0, nCandleIndex = this.Candles.Count - 3; nCandleIndex > nCandlesListStart - 2; nCandleDataIndex += nParamsPerCandle, nCandleIndex--)
//                {
//                    cCurrCandle = this.Candles[nCandleIndex];
//                    arrSample[nCandleDataIndex + 0] = cCurrCandle.EndWMA;
//                    arrSample[nCandleDataIndex + 1] = cCurrCandle.EndEMA;
//                    arrSample[nCandleDataIndex + 2] = cCurrCandle.WMADirection ? 1 : 0;
//                    arrSample[nCandleDataIndex + 3] = cCurrCandle.EMADirection ? 1 : 0;
//                    arrSample[nCandleDataIndex + 4] = cCurrCandle.Close - cCurrCandle.Open;
//                    arrSample[nCandleDataIndex + 5] = cCurrCandle.High - cCurrCandle.Low;
//                    arrSample[nCandleDataIndex + 6] = cCurrCandle.Close > cCurrCandle.Open ? cCurrCandle.Open - cCurrCandle.Low : cCurrCandle.Close - cCurrCandle.Low;
//                    arrSample[nCandleDataIndex + 7] = (cCurrCandle.Extra > double.MinValue) ? (cCurrCandle.Low - cCurrCandle.Extra) : 0;
//                    arrSample[nCandleDataIndex + 8] = cCurrCandle.NumOfPeiceUpdates;
//                    arrSample[nCandleDataIndex + 9] = cCurrCandle.EndWMA - cCurrCandle.EndEMA;
//                }

//                arrAllSampls[nSampleIndex++] = arrSample;
//            }

//            return (arrAllSampls);
//        }
        //public double[][] MakeNeuralNetworkDataMatrix4(bool bIsTrainingMatrix, int nCandlesPerSample, int nOutputCount, int nParamsPerCandle, int nOutcomeLength, bool bStrategy)
        //{
        //    List<NNOrder> Data = new List<NNOrder>();
        //    foreach (NNOrder nnoCurr in this.NeuralNetworkSelfAwarenessData)
        //    {
        //        //                if (nnoCurr.Strategy == bStrategy)
        //        {
        //            Data.Add(nnoCurr);
        //        }
        //    }

        //    int nSamplesCount = Data.Count;
        //    int nCandlesListStart = bIsTrainingMatrix ? 0 : this.Candles.Count - nCandlesPerSample - 1;
        //    int nMatrixSizeToReturn = bIsTrainingMatrix ? nSamplesCount : 1;
        //    double[][] arrAllSampls = new double[nMatrixSizeToReturn][];
        //    double[] arrSample;
        //    Candle cCurrCandle;
        //    Candle cPrevCandle;
        //    int nSampleIndex = 0;

        //    if (bIsTrainingMatrix)
        //    {
        //        foreach (NNOrder nnoCurr in Data)
        //        {
        //            arrSample = new double[nCandlesPerSample * nParamsPerCandle + nOutputCount];
        //            int nStartIndex = (nCandlesPerSample - (nnoCurr.CandlesHistory.Count - 2)) * nParamsPerCandle;

        //            cCurrCandle = nnoCurr.CandlesHistory[nnoCurr.CandlesHistory.Count - 1];
        //            for (int nCandleDataIndex = nStartIndex, nCandleIndex = nnoCurr.CandlesHistory.Count - 2; nCandleIndex > nnoCurr.CandlesHistory.Count - nCandlesPerSample - 2; nCandleDataIndex += nParamsPerCandle, nCandleIndex--)
        //            {
        //                cPrevCandle = nnoCurr.CandlesHistory[nCandleIndex];
        //                arrSample[nCandleDataIndex + 0] = MathUtils.PercentChange(cCurrCandle.EndWMA, cPrevCandle.EndWMA);
        //                arrSample[nCandleDataIndex + 1] = MathUtils.PercentChange((cCurrCandle.Close + cCurrCandle.Open) / 2,
        //                                                                          (cPrevCandle.Close + cPrevCandle.Open) / 2);
        //                arrSample[nCandleDataIndex + 2] = cPrevCandle.Close - cPrevCandle.Low;
        //                arrSample[nCandleDataIndex + 3] = MathUtils.PercentChange(cCurrCandle.NumOfPeiceUpdates, cPrevCandle.NumOfPeiceUpdates);
        //                arrSample[nCandleDataIndex + 4] = MathUtils.PercentChange(cCurrCandle.EndWMA - cCurrCandle.EndEMA,
        //                                                                          cPrevCandle.EndWMA - cPrevCandle.EndEMA);
        //                arrSample[nCandleDataIndex + 5] = cPrevCandle.WMADirection ? 1 : 0;
        //                arrSample[nCandleDataIndex + 6] = cPrevCandle.EMADirection ? 1 : 0;
        //                arrSample[nCandleDataIndex + 7] = MathUtils.PercentChange(cCurrCandle.High - cCurrCandle.Low,
        //                                                                          cPrevCandle.High - cPrevCandle.Low);

        //                arrSample[nCandleDataIndex + 8] =
        //                    cPrevCandle.ExtraList[0] > double.MinValue ? MathUtils.PercentChange(cCurrCandle.ExtraList[0] - cCurrCandle.ExtraList[1],
        //                                                                                         cPrevCandle.ExtraList[0] - cPrevCandle.ExtraList[1]) : 0;
        //                arrSample[nCandleDataIndex + 9] =
        //                    cPrevCandle.ExtraList[0] > double.MinValue ? MathUtils.PercentChange(cCurrCandle.ExtraList[2] - cCurrCandle.ExtraList[0],
        //                                                                                         cPrevCandle.ExtraList[2] - cPrevCandle.ExtraList[0]) : 0;

        //                for (int nIndicatorIndex = 3; (nIndicatorIndex < cCurrCandle.ExtraList.Count) && (nIndicatorIndex < cPrevCandle.ExtraList.Count); nIndicatorIndex++)
        //                {
        //                    arrSample[nCandleDataIndex + 10 + nIndicatorIndex] =
        //                        cPrevCandle.ExtraList[nIndicatorIndex] > double.MinValue ? MathUtils.PercentChange(cCurrCandle.ExtraList[nIndicatorIndex],
        //                                                                                                           cPrevCandle.ExtraList[nIndicatorIndex]) : 0;
        //                }
        //            }

        //            arrSample[nCandlesPerSample * nParamsPerCandle + nnoCurr.ProfitIndicator] = 1;
        //            arrAllSampls[nSampleIndex++] = arrSample;
        //        }
        //    }
        //    else
        //    {
        //        arrSample = new double[nCandlesPerSample * nParamsPerCandle + nOutputCount];

        //        cCurrCandle = this.Candles[this.Candles.Count - 2];
        //        for (int nCandleDataIndex = 0, nCandleIndex = this.Candles.Count - 3; nCandleIndex > nCandlesListStart - 2; nCandleDataIndex += nParamsPerCandle, nCandleIndex--)
        //        {
        //            cPrevCandle = this.Candles[nCandleIndex];
        //            arrSample[nCandleDataIndex + 0] = MathUtils.PercentChange(cCurrCandle.EndWMA, cPrevCandle.EndWMA);
        //            arrSample[nCandleDataIndex + 1] = MathUtils.PercentChange((cCurrCandle.Close + cCurrCandle.Open) / 2,
        //                                                                      (cPrevCandle.Close + cPrevCandle.Open) / 2);
        //            arrSample[nCandleDataIndex + 2] = cPrevCandle.Close - cPrevCandle.Low;
        //            arrSample[nCandleDataIndex + 3] = MathUtils.PercentChange(cCurrCandle.NumOfPeiceUpdates, cPrevCandle.NumOfPeiceUpdates);
        //            arrSample[nCandleDataIndex + 4] = MathUtils.PercentChange(cCurrCandle.EndWMA - cCurrCandle.EndEMA,
        //                                                                      cPrevCandle.EndWMA - cPrevCandle.EndEMA);
        //            arrSample[nCandleDataIndex + 5] = cPrevCandle.WMADirection ? 1 : 0;
        //            arrSample[nCandleDataIndex + 6] = cPrevCandle.EMADirection ? 1 : 0;
        //            arrSample[nCandleDataIndex + 7] = MathUtils.PercentChange(cCurrCandle.High - cCurrCandle.Low,
        //                                                                      cPrevCandle.High - cPrevCandle.Low);

        //            arrSample[nCandleDataIndex + 8] =
        //                cPrevCandle.ExtraList[0] > double.MinValue ? MathUtils.PercentChange(cCurrCandle.ExtraList[0] - cCurrCandle.ExtraList[1],
        //                                                                                     cPrevCandle.ExtraList[0] - cPrevCandle.ExtraList[1]) : 0;
        //            arrSample[nCandleDataIndex + 9] =
        //                cPrevCandle.ExtraList[0] > double.MinValue ? MathUtils.PercentChange(cCurrCandle.ExtraList[2] - cCurrCandle.ExtraList[0],
        //                                                                                     cPrevCandle.ExtraList[2] - cPrevCandle.ExtraList[0]) : 0;

        //            for (int nIndicatorIndex = 3; (nIndicatorIndex < cCurrCandle.ExtraList.Count) && (nIndicatorIndex < cPrevCandle.ExtraList.Count); nIndicatorIndex++)
        //            {
        //                arrSample[nCandleDataIndex + 10 + nIndicatorIndex] =
        //                    cPrevCandle.ExtraList[nIndicatorIndex] > double.MinValue ? MathUtils.PercentChange(cCurrCandle.ExtraList[nIndicatorIndex],
        //                                                                                                       cPrevCandle.ExtraList[nIndicatorIndex]) : 0;
        //            }
        //        }

        //        arrAllSampls[nSampleIndex++] = arrSample;
        //    }

        //    return (arrAllSampls);
        //}
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

        // RSI
        private double NewRSI()
        {
            double dLastChangingRate = this.Candles[this.CountDec - 1].Close -
                           this.Candles[this.CountDec - 2].Close;

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

            this.UpdateRSI();
            this.RSIList.Add(this.RSI);
            double dFirstRSI = this.RSIList[0];
            this.RSIList.RemoveAt(0);
            if (dFirstRSI == this.HighestRSI) { this.HighestRSI = this.RSIList.Max(); }
            if (dFirstRSI == this.LowestRSI) { this.LowestRSI = this.RSIList.Min(); }

            return (this.RSI);
        }
        private double UpdateRSI()
        {
            double dLastChangingRate = this.Candles[this.CountDec].Close - 
                                       this.Candles[this.CountDec - 1].Close;
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
                this.RSI = 100 - (100 / (1 + this.RS));
            }
            catch
            {
                this.RS = -1;
                this.RSI = 100;
            }

            if (this.RSI > this.HighestRSI) { this.HighestRSI = this.RSI; }
            if (this.RSI < this.LowestRSI)  { this.LowestRSI = this.RSI; }

            return (this.RSI);
        }

        // Stochastic    
        private void NewStochastic()
        {
            this.PrevStochastic = this.Stochastic;
            this.UpdateStochastic();
        }
        private void UpdateStochastic()
        {
            this.Stochastic = 100 * ((this.CurrPrice - this.LowestPrice) / (this.HighestPrice - this.LowestPrice));
        }

        // StochasticRSI
        private void NewStochasticRSI()
        {
            this.PrevStochasticRSI = this.StochasticRSI;
            this.StochasticRSI = 100 * ((this.RSI - this.LowestRSI) / (this.HighestRSI - this.LowestRSI));

            this.Stochastic_K_SmoothingList.Add(this.StochasticRSI);
            this.Stochastic_K_SmoothingList.RemoveAt(0);
            this.PrevAVGStochasticRSI_K = this.AVGStochasticRSI_K;
            this.AVGStochasticRSI_K = this.Stochastic_K_SmoothingList.Average();

            this.Stochastic_D_SmoothingList.Add(this.AVGStochasticRSI_K);
            this.Stochastic_D_SmoothingList.RemoveAt(0);
            this.PrevAVGStochasticRSI_D = this.AVGStochasticRSI_D;
            this.AVGStochasticRSI_D = this.Stochastic_D_SmoothingList.Average();
        }
        private void UpdateStochasticRSI()
        {
            this.StochasticRSI = 100 * ((this.RSI - this.LowestRSI) / (this.HighestRSI - this.LowestRSI));

            this.Stochastic_K_SmoothingList[this.StochasticParameterKsmoothingLengthDec] = (this.StochasticRSI);
            this.AVGStochasticRSI_K = this.Stochastic_K_SmoothingList.Average();
            this.Stochastic_D_SmoothingList[this.StochasticParameterDsmoothingLengthDec] = (this.AVGStochasticRSI_K);
            this.AVGStochasticRSI_D = this.Stochastic_D_SmoothingList.Average();
        }

        // SMA
        private void NewSMABand()
        {
            this.PrevSMA = this.SMA;
            this.UpdateSMABand();
        }
        private void UpdateSMABand()
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
            this.SMA = dAVG / this.SMAList.Count;
            double dStandardDeviation = Math.Sqrt(Math.Pow(this.SMA, 2) +
                                                  ((dFirstStandardDeviationOperand -
                                                   (dSecondStandardDeviationOperand * 2 * this.SMA)) /
                                                   this.SMAList.Count));
            
            // Calculate Envelope and Bollinger
            this.SMA_LowerEnvelope  = this.SMA - (this.SMA * Consts.esMA_PARAMETERS_PERCENTAGE * 0.01);
            this.SMA_UpperEnvelope  = this.SMA + (this.SMA * Consts.esMA_PARAMETERS_PERCENTAGE * 0.01);
            this.SMA_LowerBollinger = this.SMA - (dStandardDeviation * 2);
            this.SMA_UpperBollinger = this.SMA + (dStandardDeviation * 2);
        }

        // EMA
        private void NewEMA()
        {
            double dTempEMA = this.EMA;
            double dTempDerivative = this.Derivative; 
            this.PrevEMADirection = this.EMADirection;

            UpdateEMA();
            this.PrevEMA = dTempEMA;

            if (this.EMA > this.MaxEMA) { this.MaxEMA = this.EMA; }

            this.EMAList.Add(this.EMA);

            double dFirstEMA = this.EMAList[0];
            this.EMAList.RemoveAt(0);
            if      (dFirstEMA == this.MaxEMA) { this.MaxEMA = this.EMAList.Max(); }
            else if (this.EMA  >  this.MaxEMA) { this.MaxEMA = this.EMA; } 
            if      (dFirstEMA == this.MinEMA) { this.MinEMA = this.EMAList.Min(); }
            else if (this.EMA  <  this.MinEMA) { this.MinEMA = this.EMA; }

            this.PrevDerivative = dTempDerivative;
            this.DerivativeDierection = this.Derivative >= this.PrevDerivative;
        }
        private void UpdateEMA()
        {
            this.EMA = (this.CurrPrice - this.PrevEMA) * this.EMAMultiplier + this.PrevEMA;
            this.EMADirection = this.EMA > this.PrevEMA;

            this.Derivative = this.EMA / this.PrevEMA;
            this.DerivativeDierection = this.Derivative >= this.PrevDerivative;
        }

        // WMA
        private void NewWMA()
        {
            this.PrevWMA = this.WMA;
            this.PrevWMADirection = this.WMADirection;
            this.UpdateWMA();
        }
        private void UpdateWMA()
        {
            int dIndex = 1;
            this.WMA = 0;
            for (int nCounter = Consts.WMA_PARAMETERS_LENGTH; nCounter > 0; nCounter--)
            {
                this.WMA += this.Candles[this.Count - dIndex].Close * nCounter;
                dIndex++;
            }
            this.WMA /= this.WMAParametersDenominator;
            this.WMADirection = this.WMA > this.PrevWMA;
        }

        // Awesome
        private void NewAwesome()
        {
            this.PrevAwesome = this.Awesome;
            this.UpdateAwesome();
        }
        private void UpdateAwesome()
        {
            this.Awesome = ((this.AwesomeMidSMAShortList.Average(C => C.High + C.Low)) / 2) - 
                           ((this.AwesomeMidSMALongList.Average(C => C.High + C.Low)) / 2);
        }

        // TDI
        private void NewTDI()
        {
            this.PrevTDI_Green = this.TDI_Green;
            this.PrevTDI_Red = this.TDI_Red;
            this.TDIRSIList.RemoveAt(0);
            this.TDIRSIList.Add(this.RSI);
            this.UpdateTDI();
        }
        private void UpdateTDI()
        {
            int nGreenPeriod = 2;
            int nRedPeriod = 7;
            double dTempTDI_Red = 0;
            int dTDIRSIListCountDec = this.TDIRSIList.Count - 1;
            this.TDIRSIList[dTDIRSIListCountDec] = this.RSI;

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
    }
}
