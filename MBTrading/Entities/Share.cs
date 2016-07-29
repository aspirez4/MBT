using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MBTrading.Utils;
using MBTrading.Entities;
using System.Collections.Generic;

namespace MBTrading
{
    public class Pair
    {
        public int Quantity;
        public double Price;
        public Pair (int nQuantity, double dPrice)
    	{
            this.Quantity = nQuantity;
            this.Price = dPrice;
	    }
    }

    public class Share
    {
        public bool NNActive = true;
        public int D_MilitraizedZone = 10; // 10
        public double Risk = 0;
        public bool PartialMode = false;
		public bool bWasPartiald = false;
        private List<double> tempList;
        private List<double> tempListNN;

        public bool                     bDidFirstConditionHappened          = false;
        public bool                     bDidSecondConditionHappened         = false;
        public double                   StrongMinLow;
        public int                      CandleAfterStrongMinIndex           = 0;
        public int                      StartReversalIndex                  = 0;
        public ChangingPriceForOrder    ChangingStopPirce                   = null;
        public double                   ReversalStopLimitPrice              = 0;
        public bool                     CrossIndicator                      = false;
        public bool                     CrossEMALine                        = false;


        public CandlesList                          CandlesList;
        public ConcurrentQueue<MarketData>          PricesQueue;
        public Order                                BuyOrder;
        public double                               TotalPL;
        public double                               TotalPipsPL;
        public double                               TotalProfit;
        public double                               TotalLoss;
        public double                               CurrPL;
        public double                               Commission;
        public string                               Symbol;
        public bool                                 IsPosition;
        public bool                                 IsBuyOrderSentOrExecuted;
        public Dictionary<int, Pair>                BuyPrices;
        public double                               AverageBuyPrice;
        public double                               SellPrice;
        public double                               StopLoss;
        public int                                  PositionQuantity;
        public bool                                 PositionsReport;
        public ConcurrentDictionary<string, Order>  StopLossOrders;
        
        public double                               PipsUnit;
        public double                               PipsAboveForLimitPrice;
        public double                               PipsToStopLimit;
        public double                               PipsToStopLoss;
        public double                               PipsToPartial;
        public int                                  CandleIndex = 0;
        public int                                  BuyIndex = -1;
        public int                                  BuyDirection = -1;
        public double                               FirstStopLoss = -1;
        public int                                  SellIndex = -2;
         
        public bool                                 OffLineIsPosition                    = false;
        public bool                                 OffLineOrBolBuy                      = false;
        public int                                  OffLineCandleIndex                   = 0;
        public int                                  OffLineBuyIndex                      = 0;
        public int                                  OffLineSellIndex                     = 0;
        public int                                  OffLineSignalIndex                   = 0;
        public int                                  OffLineVirtualBuyIndex               = 0;

        // MachineLearning params
        public int PatternLength;
        public int PatternOutcomeIntervel;
        public int PatternOutcomeRange;

        // CTors
        static  Share(){}
        public  Share(string strSymbol)
        {
            this.PricesQueue                = new ConcurrentQueue<MarketData>();
            this.ChangingStopPirce          = new ChangingPriceForOrder();
            this.BuyOrder                   = null;
            this.TotalPipsPL                = 0;
            this.CurrPL                     = 0;
            this.TotalPL                    = 0;
            this.TotalProfit                = 0;
            this.TotalLoss                  = 0;
            this.Commission                 = 0;
            this.PositionQuantity           = 0;
            this.BuyPrices                  = new Dictionary<int, Pair>();
            this.AverageBuyPrice            = 0;
            this.SellPrice                  = 0;
            this.StopLoss                   = 0;
            this.StopLossOrders             = new ConcurrentDictionary<string, Order>();
            this.IsPosition                 = false;
            this.IsBuyOrderSentOrExecuted   = false;
            this.Symbol                     = strSymbol;
            this.PositionsReport            = false;
            this.tempList                   = new List<double>();
            this.tempListNN                 = new List<double>();
            for (int i = 0; i < Consts.NEURAL_NETWORK_MA_LENGTH; i++) { this.tempList.Add(0); }
            for (int i = 0; i < Consts.NEURAL_NETWORK_MA_LENGTH; i++) { this.tempListNN.Add(0); }
        }
        public  void UpdateShareConsts()
        {
            PipsUnit                = 0.0001;
            PipsAboveForLimitPrice  = 0.0001 * Consts.PIPS_ABOVE_FOR_LIMIT_PRICE;
            PipsToStopLimit         = 0.0001 * Consts.PIPS_TO_STOP_LIMIT;
            PipsToStopLoss          = 0.0001 * Consts.PIPS_TO_STOP_LOSS;
            PipsToPartial           = 0.0001 * Consts.PIPS_TO_PARTIAL;

            if (this.Symbol.Contains("JPY")) 
            {
                PipsUnit                = 0.01;
                PipsAboveForLimitPrice  = 0.01 * Consts.JPY_PIPS_ABOVE_FOR_LIMIT_PRICE;
                PipsToStopLimit         = 0.01 * Consts.JPY_PIPS_TO_STOP_LIMIT;
                PipsToStopLoss          = 0.01 * Consts.JPY_PIPS_TO_STOP_LOSS;
                PipsToPartial           = 0.01 * Consts.JPY_PIPS_TO_PARTIAL;
            }
        }
        public  void InitializeShere()
        {
            Loger.ExecutionReport(this.Symbol, null, false, "Initilizing");
            new Thread(InitializeProcedure).Start();
        }
        private void InitializeProcedure()
        {
            this.CancelAllStopLossOrders();
            this.UpdateShareConsts();

            this.CrossIndicator = false;
            this.OffLineIsPosition = false;
            this.StrongMinLow = double.MaxValue;

            this.PositionQuantity = 0;
            this.BuyPrices.Clear();
            this.OffLineBuyIndex = 0;
            this.OffLineSellIndex = 0;
            this.BuyIndex = 0;
            this.SellIndex = 0;
            this.AverageBuyPrice = 0;
            this.StopLoss = 0;
            this.CurrPL = 0;
            this.PositionsReport = false;

            this.FirstStopLoss = -1;
            this.BuyOrder = null;
            this.IsPosition = false;
            this.IsBuyOrderSentOrExecuted = false;
            Loger.ExecutionReport(this.Symbol, null, false, "Initilized");
        }



        // Online Mode
        public  void Activate()
        {
            Console.WriteLine(this.Symbol + "  Was Activated");
            this.UpdateShareConsts();

            MarketData mdCurrMarketData = null;
            while (Program.IsProgramAlive)
            {
                if (this.PricesQueue.TryDequeue(out mdCurrMarketData))
                {
                    this.UpdateCandle(mdCurrMarketData);
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }
        public void UpdateCandle(MarketData mdCurrMarketData)
        {
            // If it's a NEW candle!!!
            if (this.CandlesList.AddOrUpdatePrice(mdCurrMarketData))
            {
                #region New Candle - Initialize stuff
                this.CandleIndex = this.CandleIndex == int.MaxValue ? 0 : this.CandleIndex + 1;
                if ((this.BuyOrder != null) && (this.CandleIndex == this.BuyOrder.CandleIndexTTL))
                {
                    this.BuyOrder.CancelOrder_NewThread();
                }

                this.bDidFirstConditionHappened = false;
                this.bDidSecondConditionHappened = false;

                Candle cMegaPreviousCandle = this.CandlesList.Candles[this.CandlesList.CountDec - 3];
                Candle cBeforePreviousCandle = this.CandlesList.Candles[this.CandlesList.CountDec - 2];
                Candle cPreviousCandle = this.CandlesList.Candles[this.CandlesList.CountDec - 1];
                #endregion

                #region Reversal
                // Maybe Reversal is possible
                if ((this.StartReversalIndex != 0) && (cBeforePreviousCandle.WMADirection) && (!cPreviousCandle.WMADirection))
                {
                    // MAs not fitts (WMA is above EMA) - Cancel the reversal option scenario
                    if (cBeforePreviousCandle.EndWMA > cBeforePreviousCandle.EndEMA)
                    {
                        this.StartReversalIndex = 0;
                    }
                    else
                    {
                        int nStartIndex = this.CandleIndex - this.StartReversalIndex;

                        // if indexes are in bounds
                        if (this.CandlesList.Candles.Count - nStartIndex - 4 >= 0)
                        {
                            double dReversalFirstHigh = 0;
                            double dHighiestPriceBeforePossibility = 0;

                            // | <<< (dHighiestPriceBeforePossibility)
                            // |  
                            // ||
                            // ||  Buy according strategy and imidiate sell => possibility?! (StartReversalIndex)
                            // ||  V
                            // ||  V
                            // ||  V
                            // || ||
                            // || || |        ___________________ <<< (dReversalFirstHigh)
                            // |  || ||       ||
                            //    || ||    || || ||
                            //    |  || || ||    || ||
                            //    |     |           ||
                            //    |     |
                            //    |______________________________ <<< (ReversalStopLossPrice)
                            //    
                            //
                            // Get the first high hill
                            for (int nMaxStopIndex = this.CandlesList.Candles.Count - nStartIndex; nMaxStopIndex < this.CandlesList.Candles.Count; nMaxStopIndex++)
                            {
                                dReversalFirstHigh = this.CandlesList.Candles[nMaxStopIndex].High > dReversalFirstHigh ? this.CandlesList.Candles[nMaxStopIndex].High : dReversalFirstHigh;
                            }

                            // Get the Highiest price befor the possibility
                            for (int nMaxStopIndex = this.CandlesList.Candles.Count - nStartIndex - 4; nMaxStopIndex < this.CandlesList.Candles.Count - nStartIndex; nMaxStopIndex++)
                            {
                                dHighiestPriceBeforePossibility = this.CandlesList.Candles[nMaxStopIndex].High > dHighiestPriceBeforePossibility ? this.CandlesList.Candles[nMaxStopIndex].High : dHighiestPriceBeforePossibility;
                            }

                            // if first high is in range
                            if (dHighiestPriceBeforePossibility > dReversalFirstHigh)
                            {
                                this.StrongMinLow = double.MaxValue;
                                this.ReversalStopLimitPrice = dReversalFirstHigh;
                                this.ChangingStopPirce.ReversalStopLossPrice = this.CandlesList.Candles[this.CandlesList.Candles.Count - (this.CandleIndex - this.StartReversalIndex)].Low;

                                // Set Buy Order
                                if (!this.IsBuyOrderSentOrExecuted)
                                {
                                    ///////////////////////////////////////////////////////////////////////////////
                                    /////////////////////////////////LIVE-ACTION!!!////////////////////////////////
                                    /////////////SET STOPLIMIT && STOPLOSS - POSSIBILITY TO REVERSAL///////////////
                                    ///////////////////////////////////////////////////////////////////////////////
                                    this.IsBuyOrderSentOrExecuted = true;
                                    this.BuyStopLimitPlusStopLoss(this.ReversalStopLimitPrice,
                                                                  this.ReversalStopLimitPrice + PipsToStopLimit,
                                                                  this.ChangingStopPirce,
                                                                  "ReversalStopLossPrice",
                                                                  Program.Quantity,
                                                                  "Reversal",
                                                                  null);
                                }
                            }
                            // if first high is in not in range - Cancel the reversal 
                            else
                            {
                                this.StartReversalIndex = 0;
                            }
                        }
                        else
                        {
                            Loger.ExecutionReport(this.Symbol, null, false, "Just stupid debbuger log...the indexes of CandleList wasn't in bounds");
                        }
                    }
                }
                #endregion

                #region Strategy - 'StrongMin' First condition
                // First condition of strategy happened - 'StrongMin'
                if ((cPreviousCandle.Low < this.CandlesList.SMA.LowerBollinger) &&
                    (cPreviousCandle.Open - cPreviousCandle.Close) > this.CandlesList.SMA.CandleSizeAVG)
                {
                    this.StrongMinLow = cPreviousCandle.Low;
                    this.CandleAfterStrongMinIndex = this.CandleIndex;
                    this.bDidFirstConditionHappened = true;
                    this.bDidSecondConditionHappened = false;
                    this.ChangingStopPirce.StrategyStopLossPrice = double.MaxValue;
                    this.StartReversalIndex = 0;
                    this.ReversalStopLimitPrice = double.MaxValue;
                    this.ChangingStopPirce.ReversalStopLossPrice = 0;
                }
                #endregion

                #region My Bollinger strategy
                if ((!this.IsPosition) &&
                    (!this.IsBuyOrderSentOrExecuted) &&
                    (this.StartReversalIndex == 0) &&
                    (!this.bDidFirstConditionHappened) &&
                    (cPreviousCandle.Low < this.CandlesList.SMA.LowerBollinger) &&
                    (cPreviousCandle.Close > this.CandlesList.SMA.LowerBollinger) &&
                    (cPreviousCandle.Open > this.CandlesList.SMA.LowerBollinger) && 
                    (cPreviousCandle.Close > cPreviousCandle.Open) && 
                    (cPreviousCandle.StartWMA < this.CandlesList.WMA.Value))
                {
                    ///////////////////////////////////////////////////////////////////////////////
                    /////////////////////////////////LIVE-ACTION!!!////////////////////////////////
                    /////////////SET STOPLIMIT && STOPLOSS - POSSIBILITY TO REVERSAL///////////////
                    ///////////////////////////////////////////////////////////////////////////////
                    this.IsBuyOrderSentOrExecuted = true;
                    this.ChangingStopPirce.BollingerStopLossPrice = cPreviousCandle.Low;
                    this.bDidSecondConditionHappened = this.BuyStopLimitPlusStopLoss(this.CandlesList.CurrPrice,
                                                                                     this.CandlesList.CurrPrice + PipsToStopLimit,
                                                                                     this.ChangingStopPirce,
                                                                                     "BollingerStopLossPrice",
                                                                                     Program.Quantity,
                                                                                     "MyBollingerStrategy",
                                                                                     this.CandleIndex + 1);
                }
                #endregion

                #region Update Stoploss
                // This section waites from the moment that MA's cross each other, to the moment that WMA direction is changing UP again but still bellow the EMA
                // Potential Sell conditions - MA's crossed - WMA bellow EMA - Starting of a Downward
                bool bCrossMA = ((this.CandlesList.PrevCandle.StartWMA - this.CandlesList.PrevCandle.StartEMA > (this.PipsUnit * 3)) && (this.CandlesList.WMA.Value - this.CandlesList.EMA.Value <= (this.PipsUnit * 3)));

                // Set the indicator to True wen => Cross occurd || already set it befor to True
                this.CrossIndicator = (((bCrossMA) && (this.IsPosition)) || (this.CrossIndicator));

                // If cross occourd (Both on STRATEGY position or in REVERSAL position), update the stoploss in chance of little reversal
                if ((this.IsPosition) && (this.CrossIndicator) && (!cBeforePreviousCandle.WMADirection) && (cPreviousCandle.WMADirection) && (this.CandlesList.EMA.Value > this.CandlesList.WMA.Value))
                {
                    ///////////////////////////////////////////////////////////////////////////////
                    /////////////////////////////////LIVE-ACTION!!!////////////////////////////////
                    ///////////////////////////////////////////////////////////////////////////////
                    this.UpdateAllStopLossOrders(Math.Min(cBeforePreviousCandle.Low, cPreviousCandle.Low) - (this.PipsUnit * 2), this.PositionQuantity);
                    this.CrossIndicator = false;
                }
                #endregion
            }
            // If it's an UPDATE candle!!!
            else
            {
                #region Strategy - Second condition
                // Second condition of strategy happened - 4pips candle get below 'StrongMin'  -   Set Buy Order
                if ((!this.IsPosition) && (this.CandlesList.CurrPrice + (4 * this.PipsUnit) < this.StrongMinLow) && (this.bDidFirstConditionHappened))
                {
                    this.ChangingStopPirce.StrategyStopLossPrice = this.CandlesList.CurrPrice < this.ChangingStopPirce.StrategyStopLossPrice ? this.CandlesList.CurrPrice : this.ChangingStopPirce.StrategyStopLossPrice;

                    // If Strategy BUY order hasn't been send already
                    if (!this.bDidSecondConditionHappened)
                    {
                        // If there is other order (the only order that could be at this moment is buy order of reversalPossibility) - Cancel it (because stategy buy orders are Preferred on reversalPossibility orders)
                        if ((this.BuyOrder != null) && (!this.BuyOrder.IsCancelSent))
                        {
                            this.BuyOrder.CancelOrder_NewThread();
                        }
                        else if (!this.IsBuyOrderSentOrExecuted)
                        {
                            ///////////////////////////////////////////////////////////////////////////////
                            /////////////////////////////////LIVE-ACTION!!!////////////////////////////////
                            /////////////SET STOPLIMIT && STOPLOSS - POSSIBILITY TO REVERSAL///////////////
                            ///////////////////////////////////////////////////////////////////////////////
                            this.IsBuyOrderSentOrExecuted = true;
                            this.bDidSecondConditionHappened = this.BuyStopLimitPlusStopLoss(this.StrongMinLow, 
                                                                                             this.StrongMinLow + PipsToStopLimit, 
                                                                                             this.ChangingStopPirce, 
                                                                                             "StrategyStopLossPrice",
                                                                                             Program.Quantity, 
                                                                                             "Strategy", 
                                                                                             this.CandleIndex + 1);
                        }
                    }
                }
                #endregion

                #region Sold at same candle - Reversal possibility
                // if Cross the Stoploss at the same index of the BuyOrder - So Reversal is possible
                if (this.BuyIndex == this.SellIndex)
                {
                    this.StartReversalIndex = this.CandleIndex;
                    this.ReversalStopLimitPrice = double.MaxValue;
                }
                #endregion
            }          
        }



        public void BuyStopLimitPlusTrailing(double dLimit, double dStop, double dTrailingInterval, int nQuantitiy)
        {
      
        }
        public bool BuyStopLimitPlusStopLoss(double dLimit, 
                                             double dStopLimit, 
                                             ChangingPriceForOrder dChangingStopLoss, 
                                             string strStopLossReferencePropName, 
                                             int    nQuantitiy, 
                                             string strLogMessage, 
                                             int?   nOrderLastInThisSpecificCandleIndex)
        {
            return (FixGatewayUtils.BuyStopLimit(this.Symbol, dLimit, dStopLimit, nQuantitiy, strStopLossReferencePropName, nOrderLastInThisSpecificCandleIndex, strLogMessage));
        }
        public bool     SellMarket()
        {
            return (FixGatewayUtils.SellMarket(this.Symbol, this.PositionQuantity));
        }
        public void     SellPartial(int nQuantityForPartial, bool bUpdateStopLossAlso)
        {
            //int nCurrQuantity = this.PositionQuantity;

            //Loger.ExecutionsReport(this.Symbol + " Partial " + (nQuantityForPartial));
            //FixGatewayUtils.SellMarket(this.Symbol, nQuantityForPartial);

            //if (bUpdateStopLossAlso)
            //{
            //    Loger.ExecutionsReport(this.Symbol + " Update quantity after partial ");
            //    this.UpdateAllStopLossOrders(null, nCurrQuantity - nQuantityForPartial);
            //}
        }
        public  void    UpdateAllStopLossOrders(double? dStop, int? nQuantity)
        {
            double dPrevStopLoss = this.StopLoss;

            foreach (Order oStopOrder in this.StopLossOrders.Values)
            {
                oStopOrder.UpdateStopLossOrder_NewThread(dStop, nQuantity);
            }

            double dPipsChange = dStop != null ? this.StopLoss - dPrevStopLoss : 0;

            Loger.ExecutionReport(this.Symbol, null, false, string.Format("Updating all stopLosses ({0}) - {1}Pips", this.StopLossOrders.Count, this.PipsUnit * dPipsChange));
        }
        public  void    CancelAllStopLossOrders()
        {
            Loger.ExecutionReport(this.Symbol, null, false, string.Format("Canceling all stopLosses ({0})", this.StopLossOrders.Count));
            foreach (Order oStopOrder in this.StopLossOrders.Values)
            {
                oStopOrder.CancelOrder_NewThread();
            }
        }




        // Didnt SCOD it!!!
        public bool LimitSell(double dLimit)
        {
            if (FixGatewayUtils.LogonIndicator)
            {
                FixGatewayUtils.SellLimit(this.Symbol, dLimit, this.PositionQuantity);
                return (true);
            }

            return (false);
        }
        // מרגע שליחת פקודת הקניה...כל 60 שניות, צריך לבדוק אם מחיר מאיזושהי סיבה, המחיר הנוכחי ירד מתחת למחיר הסטופ והמניה עדיין פוזיציה
        public  void   SecurePosition()
        {
            Thread.Sleep(60000);
            while (this.IsPosition)
            {
                if (this.CandlesList.CurrPrice < this.StopLoss)
                {
                    Thread.Sleep(10000);
                    Share.PositionStatus();

                    if ((this.PositionsReport) && (this.IsPosition) && (this.CandlesList.CurrPrice < this.StopLoss))
                    {
                        // TODO: SellShareInMarket
                        this.SellMarket();
                    }
                }

                Thread.Sleep(60000);
            }
        }
        public  static void PositionStatus()
        {
            FixGatewayUtils.RequestForPositions();
            Thread.Sleep(10000);

            foreach (Share sCurrShare in Program.SharesList.Values)
            {
                if ((!sCurrShare.PositionsReport) && (sCurrShare.IsPosition))
                {
                    sCurrShare.InitializeShere();
                }
            }        
        }



        public double FindTheLastKnee(int nNumOfCandlesToStartBack, bool bLong)
        {
            int nSearchDirection = bLong ? 1 : -1;
            double dKnee = this.CandlesList.Candles[this.CandlesList.CountDec - nNumOfCandlesToStartBack].R_Low;

            bool bWMADir = this.CandlesList.Candles[this.CandlesList.CountDec - nNumOfCandlesToStartBack].WMADirection;
            for (int nWMADirIndex = this.CandlesList.CountDec - nNumOfCandlesToStartBack - 1; nWMADirIndex > 0; nWMADirIndex--)
            {
                if (bLong)
                {
                    if ((!bWMADir) && (this.CandlesList.Candles[nWMADirIndex].WMADirection))
                        break;

                    if (dKnee > this.CandlesList.Candles[nWMADirIndex].R_Low)
                        dKnee = this.CandlesList.Candles[nWMADirIndex].R_Low;
                }
                else
                {
                    if ((bWMADir) && (!this.CandlesList.Candles[nWMADirIndex].WMADirection))
                        break;

                    if (dKnee < this.CandlesList.Candles[nWMADirIndex].R_High)
                        dKnee = this.CandlesList.Candles[nWMADirIndex].R_High;
                }
                bWMADir = this.CandlesList.Candles[nWMADirIndex].WMADirection;
            }

            return (dKnee + (5 * this.PipsUnit * nSearchDirection));
        }
        public double CalcStopLoss()
        {
            double dStopLoss = this.CandlesList.LastCandle.R_Low;

            bool bWMADir = this.CandlesList.Candles[this.CandlesList.CountDec].WMADirection;
            for (int nWMADirInex = this.CandlesList.Candles.Count - 2; nWMADirInex > 0 /* this.CandlesList.CountDec - 10 */ ; nWMADirInex--)
            {
                if ((bWMADir) && !(this.CandlesList.Candles[nWMADirInex].WMADirection) && dStopLoss > this.CandlesList.Candles[nWMADirInex].Low)
                { dStopLoss = this.CandlesList.Candles[nWMADirInex].Low; /* break; */ }
                bWMADir = this.CandlesList.Candles[nWMADirInex].WMADirection;
            }

            return (dStopLoss - 5 * this.PipsUnit);
        }
        public void OffLineActivate()
        {
            Console.WriteLine(this.Symbol + "  Was Activated OffLine");
            this.UpdateShareConsts();

            MarketData mdCurrMarketData = null;
            while (Program.IsProgramAlive)
            {
                if (this.PricesQueue.TryDequeue(out mdCurrMarketData))
                {
                    this.OffLineUpdateCandle(mdCurrMarketData);
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }
        public void OffLineUpdateCandle(MarketData mdCurrMarketData)
        {
            #region init
            bool bIsNewCandle = this.CandlesList.AddOrUpdatePrice(mdCurrMarketData);
            if (bIsNewCandle)
            {
                Thread.Sleep(100);
                this.OffLineCandleIndex++;

                if ((Consts.NEURAL_NETWORK_TRAINING_INTERVAL == 0) && (this.NNActive))
                {
                    if (this.OffLineCandleIndex % Consts.NEURAL_NETWORK_CHANK_SIZE == 0)
                        this.CandlesList.NeuralNetworkActivate();
                }
                else
                {
                    if (this.OffLineCandleIndex % Consts.NEURAL_NETWORK_TRAINING_INTERVAL == Consts.NEURAL_NETWORK_CHANK_SIZE + 10)
                        this.CandlesList.NeuralNetworkActivate();
                }


                if (this.CandlesList.NeuralNetworkRawData.Count > Consts.NEURAL_NETWORK_MA_LENGTH)
                {
                    int nOffset = this.CandlesList.NeuralNetworkRawData.Count - Consts.NEURAL_NETWORK_MA_LENGTH;
                    for (int nTempIndex = 0; nTempIndex < Consts.NEURAL_NETWORK_MA_LENGTH; nTempIndex++)
                    {
                        this.tempList[nTempIndex] = this.CandlesList.NeuralNetworkRawData[nOffset + nTempIndex];
                    }
                    File.AppendAllText(string.Format("C:\\Users\\Or\\Projects\\MBTrading - Graph\\WindowsFormsApplication1\\bin\\x64\\Debug\\b\\o{1}.txt", Consts.FilesPath, this.Symbol.Remove(3, 1)),
                        string.Format("8;{0};{1};{2}\n", this.Symbol, this.tempList.Average(), this.OffLineCandleIndex - 1));
                }
                if (this.CandlesList.NN != null)
                {
                    List<double> lstTempNormlized = this.CandlesList.NN.KondratenkoKuperinNormalizeAsModuleTrainingSet(this.tempList);
                    this.CandlesList.LastCandle.Prediction = this.CandlesList.NeuralNetworPredict(lstTempNormlized[lstTempNormlized.Count - 1], lstTempNormlized.Average());
                    this.tempListNN.Add(this.CandlesList.LastCandle.Prediction);
                    this.tempListNN.RemoveAt(0);

                    File.AppendAllText(string.Format("C:\\Users\\Or\\Projects\\MBTrading - Graph\\WindowsFormsApplication1\\bin\\x64\\Debug\\b\\o{1}.txt", Consts.FilesPath, this.Symbol.Remove(3, 1)),
                        string.Format("9;{0};{1};{2}\n", this.Symbol, this.CandlesList.LastCandle.Prediction, this.OffLineCandleIndex));
                }
            }
            #endregion













            
            if ((bIsNewCandle) && (this.CandlesList.NN != null) && (!this.OffLineIsPosition))
            {

                if (Math.Abs(this.CandlesList.LastCandle.Prediction) > 0) // && this.CandlesList.WMA.PrevDirection) ||
      //              ((this.CandlesList.LastCandle.Prediction < 0) && !this.CandlesList.WMA.PrevDirection)) // && (this.CandlesList.CurrPrice > this.CandlesList.ATR.LongValue - 5 * this.PipsUnit)) 
                {
                    ////////////////////////// Quentity ----> (int)(Program.Quantity * Math.Abs(2 * this.CandlesList.LastCandle.Prediction / 0.001))
                    OffLineBuy(this.CandlesList.WMA.PrevDirection ? this.CandlesList.ATR.ChandelierLongValue : this.CandlesList.ATR.ChandelierShortValue, Program.Quantity, this.CandlesList.LastCandle.Prediction > 0);
                    this.FirstStopLoss = FindTheLastKnee(1, this.BuyDirection == 1);
                }
            }
            else if ((bIsNewCandle) && (this.OffLineIsPosition))
            {
                this.Risk = Math.Abs(FixGatewayUtils.CalculateProfit(this.FirstStopLoss, this.CandlesList.LastCandle.Bid, this.Symbol, this.PositionQuantity));
// ATR /////////////////////////////////////////////////
//                double tempATR = this.CandlesList.ATR.LongValue - 5 * this.PipsUnit;
//                this.StopLoss = (this.StopLoss < tempATR) ? tempATR : this.StopLoss;
// ATR /////////////////////////////////////////////////

                Candle cMegaPreviousCandle = this.CandlesList.Candles[this.CandlesList.CountDec - 3];
                Candle cBeforePreviousCandle = this.CandlesList.Candles[this.CandlesList.CountDec - 2];
                Candle cPreviousCandle = this.CandlesList.Candles[this.CandlesList.CountDec - 1];

                #region Update stoploss
                // This section waites from the moment that MA's cross each other, to the moment that WMA direction is changing UP again but still bellow the EMA
                // Potential Sell conditions - MA's crossed - WMA bellow EMA - Starting of a Downward
                bool bCrossMA = ((this.CandlesList.PrevCandle.StartWMA - this.CandlesList.PrevCandle.StartEMA > this.PipsUnit * this.D_MilitraizedZone) && (this.CandlesList.WMA.Value - this.CandlesList.EMA.Value <= this.PipsUnit * this.D_MilitraizedZone));

                // Set the indicator to True when => Cross occurd || already set it befor to True
                this.CrossIndicator = (((bCrossMA) && (this.OffLineIsPosition)) || (this.CrossIndicator));

           
                    // this.StopLoss = Math.Min(cBeforePreviousCandle.R_Low, cPreviousCandle.R_Low) - (this.PipsUnit * 2);
                    // this.CrossIndicator = false;
                    double dTemp = FindTheLastKnee(1, this.BuyDirection == 1);
                    if (this.FirstStopLoss == -1)
                    {
                        if ((!cBeforePreviousCandle.WMADirection && cPreviousCandle.WMADirection && this.BuyDirection == 1) || // && this.CrossIndicator && this.CandlesList.EMA.Value > this.CandlesList.WMA.Value)
                            (cBeforePreviousCandle.WMADirection && !cPreviousCandle.WMADirection && this.BuyDirection == -1))
                        {
                            if (this.BuyDirection * (this.StopLoss - dTemp) < 0)
                            {
                                this.StopLoss = dTemp;
                                // Same prediction as the original direction
                                if (this.CandlesList.LastCandle.Prediction * this.BuyDirection > 0)
                                {
                                    OffLineBuy(this.StopLoss, (int)(Program.Quantity), this.BuyDirection == 1);
                                }
                                File.AppendAllText(string.Format("C:\\Users\\Or\\Projects\\MBTrading - Graph\\WindowsFormsApplication1\\bin\\x64\\Debug\\b\\o{1}.txt", Consts.FilesPath, this.Symbol.Remove(3, 1)),
                                    string.Format("4;{0};{1};{2}\n", this.Symbol, this.StopLoss, this.OffLineCandleIndex));
                            }
                        }
                    }
                    else
                    {
                        this.StopLoss = this.BuyDirection == 1 ? this.CandlesList.ATR.ChandelierLongValue : this.CandlesList.ATR.ChandelierShortValue;
                        if (this.BuyDirection * (this.FirstStopLoss - dTemp) < 0)
                        {
                            this.StopLoss = dTemp;
                            this.FirstStopLoss = -1;
                        }
                    }
                

                #endregion
            }


            double dProfit = this.BuyDirection * FixGatewayUtils.CalculateProfit(this.AverageBuyPrice, this.CandlesList.LastCandle.Bid, this.Symbol, this.PositionQuantity);
            if ((this.FirstStopLoss != -1) && (dProfit > this.Risk) && (this.PositionQuantity / 2 > 1000))
            {
                OffLinePartialSell(this.PositionQuantity / 2);
            }
            //if ((OffLineIsPosition) && (this.CandlesList.LastCandle.Prediction < this.CandlesList.PrevCandle.Prediction)) // || (this.CandlesList.CurrPrice <= this.StopLoss)))
            if ((OffLineIsPosition) && (this.BuyDirection * (this.CandlesList.CurrPrice - this.StopLoss) <= 0))
            {
                OffLineSell();
            }
            /*
            #region Long!
            if (bIsNewCandle)
            {
                Candle cMegaPreviousCandle = this.CandlesList.Candles[this.CandlesList.CountDec - 3];
                Candle cBeforePreviousCandle = this.CandlesList.Candles[this.CandlesList.CountDec - 2];
                Candle cPreviousCandle = this.CandlesList.Candles[this.CandlesList.CountDec - 1];

                if ((this.OffLineIsPosition) && (cPreviousCandle.EndWMA > cPreviousCandle.EndEMA) && (cBeforePreviousCandle.StartWMA < cBeforePreviousCandle.StartEMA) && (this.StopLoss < this.BuyPrice))
                {
                    this.StopLoss = this.BuyPrice;
                }

                if (!this.OffLineIsPosition)
                {
                    this.OffLineBuy(this.FindTheLastKnee(1), false);
                }

                #region Sell conditions
                // Sell conditions
                bool bCrossSL = (this.CandlesList.CurrPrice <= this.StopLoss);
                bool bTDISell = ((cPreviousCandle.EndTDI_Green < cPreviousCandle.EndTDI_Red) &&
                                 (cBeforePreviousCandle.EndTDI_Green > cBeforePreviousCandle.EndTDI_Red) &&
                                 (cPreviousCandle.EndTDI_Green > cPreviousCandle.EndTDI_Mid));

                // Sell
                // if ((OffLineIsPosition && this.bActiveLong) && ((bCrossMA_Soft) || (bCrossMA_Stif) || (bCrossMA_Stop) || (bCrossSL)))
                if (OffLineIsPosition && ((bCrossSL) || (false)))
                {
                    this.OffLineSell(true);
                }
                #endregion

                #region Update stoploss
                // This section waites from the moment that MA's cross each other, to the moment that WMA direction is changing UP again but still bellow the EMA
                // Potential Sell conditions - MA's crossed - WMA bellow EMA - Starting of a Downward
                bool bCrossMA = ((this.CandlesList.PrevCandle.StartWMA - this.CandlesList.PrevCandle.StartEMA > this.PipsUnit * this.D_MilitraizedZone) && (this.CandlesList.WMA.Value - this.CandlesList.EMA.Value <= this.PipsUnit * this.D_MilitraizedZone));

                // Set the indicator to True when => Cross occurd || already set it befor to True
                this.CrossIndicator = (((bCrossMA) && (this.OffLineIsPosition)) || (this.CrossIndicator));

                if (!cBeforePreviousCandle.WMADirection && cPreviousCandle.WMADirection && this.CrossIndicator && this.OffLineIsPosition && this.CandlesList.EMA.Value > this.CandlesList.WMA.Value)
                {
                    this.StopLoss = Math.Min(cBeforePreviousCandle.R_Low, cPreviousCandle.R_Low) - (this.PipsUnit * 2);
                    this.CrossIndicator = false;
                }
                #endregion
            }
            else
            {
                // Sell if below stopLoss
                if ((OffLineIsPosition) && (this.CandlesList.CurrPrice <= this.StopLoss))
                {
                    this.OffLineSell(true);
                }
            }
            #endregion
            */
        }
        public void OffLineBuy(double dStopLoss, int nQuantity, bool bLong)
        {
            // BUYYYYYYYYYYY
            this.BuyDirection = bLong ? 1 : -1;
            this.StartReversalIndex = 0;
            this.OffLineBuyIndex = this.OffLineBuyIndex == 0 ? this.OffLineCandleIndex : this.OffLineBuyIndex;

            this.BuyPrices.Add(this.OffLineCandleIndex, new Pair(nQuantity, this.CandlesList.LastCandle.Ask));
            this.AverageBuyPrice = 0;
            foreach (Pair pBuy in this.BuyPrices.Values)
                this.AverageBuyPrice += pBuy.Price;
            this.AverageBuyPrice /= this.BuyPrices.Count;

            this.OffLineIsPosition = true;
            this.StopLoss = dStopLoss;

            this.Risk = this.CandlesList.LastCandle.Ask - this.StopLoss;

            this.PositionQuantity += nQuantity;
            this.Commission += FixGatewayUtils.CalculateCommission(this.CandlesList.LastCandle.Ask, this.Symbol, this.PositionQuantity);

            File.AppendAllText(string.Format("C:\\Users\\Or\\Projects\\MBTrading - Graph\\WindowsFormsApplication1\\bin\\x64\\Debug\\b\\o{1}.txt", Consts.FilesPath, this.Symbol.Remove(3, 1)),
                string.Format("1;{0};{1};{2}\n", this.Symbol, this.CandlesList.LastCandle.Ask, this.OffLineCandleIndex));
        }
        public void OffLineSell()
        {
            // SELLLLLLLLLL
            this.UpdateShareConsts();
            this.FirstStopLoss = -1;
            this.SellPrice = this.CandlesList.LastCandle.Bid;
            this.OffLineSellIndex = this.OffLineCandleIndex;
            this.CrossIndicator = false;
            this.CrossEMALine = false;
            this.OffLineIsPosition = false;
			this.bWasPartiald = false;
            this.OffLineBuyIndex = 0;
            
            this.Commission += FixGatewayUtils.CalculateCommission(this.CandlesList.LastCandle.Bid, this.Symbol, this.PositionQuantity);
            double dPL = 0;
            foreach (Pair pBuy in this.BuyPrices.Values)
            {
                dPL += FixGatewayUtils.CalculateProfit(pBuy.Price, this.CandlesList.LastCandle.Bid, this.Symbol, pBuy.Quantity);
                this.TotalPipsPL += (this.BuyDirection * (this.CandlesList.LastCandle.Bid - pBuy.Price)) / this.PipsUnit;
            }
            dPL = dPL * this.BuyDirection;
            MongoDBUtils.DBEventAfterPositionSell(Program.AccountBallance, this.Symbol, dPL, this.OffLineCandleIndex, this.OffLineCandleIndex - this.OffLineBuyIndex, this.AverageBuyPrice, this.CandlesList.LastCandle.Bid);
            this.TotalPL += dPL;
            if (dPL < 0)
            { this.TotalLoss += dPL; }
            else
            { this.TotalProfit += dPL; }
            this.BuyPrices.Clear();
            this.StopLoss = 0;
            this.PositionQuantity = 0;
            File.AppendAllText(string.Format("C:\\Users\\Or\\Projects\\MBTrading - Graph\\WindowsFormsApplication1\\bin\\x64\\Debug\\b\\o{1}.txt", Consts.FilesPath, this.Symbol.Remove(3, 1)),
                string.Format("0;{0};{1};{2};{3}\n", this.Symbol, this.CandlesList.LastCandle.Bid, this.BuyDirection, this.OffLineCandleIndex));
        }
        public void OffLinePartialSell(int nQuentity)
        {
            // SELLLLLLLLLL
            this.Commission += FixGatewayUtils.CalculateCommission(this.CandlesList.LastCandle.Bid, this.Symbol, this.PositionQuantity);
            double dPL = 0;
            foreach (Pair pBuy in this.BuyPrices.Values)
            {
                pBuy.Quantity = pBuy.Quantity - nQuentity;
                dPL += FixGatewayUtils.CalculateProfit(pBuy.Price, this.CandlesList.LastCandle.Bid, this.Symbol, nQuentity);
                this.TotalPipsPL += (this.BuyDirection * (this.CandlesList.LastCandle.Bid - pBuy.Price)) / this.PipsUnit;
            }
            dPL = dPL * this.BuyDirection;
            MongoDBUtils.DBEventAfterPositionSell(Program.AccountBallance, this.Symbol, dPL, this.OffLineCandleIndex, this.OffLineCandleIndex - this.OffLineBuyIndex, this.AverageBuyPrice, this.CandlesList.LastCandle.Bid);
            this.TotalPL += dPL;
            if (dPL < 0)
            { this.TotalLoss += dPL; }
            else
            { this.TotalProfit += dPL; }

            this.PositionQuantity = this.PositionQuantity - nQuentity;
        }
        public void ZigZagLowEvent(int nIndex, double dLastLow)
        {
            //double dPossibleStopLoss = 0;

            //if (this.IsPosition)
            //{
            //    if ((this.StopLoss < dLastLow) && (this.CandleIndex - this.BuyIndex > this.CandlesList.ZigZag5.Length - nIndex))
            //    {
            //        this.StopLoss = dLastLow;
            //    }
            //}
            //else if ((this.OffLineIsPosition) && (this.CrossEMALine))
            //{
            //    for (int nZigZagIndex = nIndex - 1; nZigZagIndex > 0; nZigZagIndex--)
            //    {
            //        if (this.CandlesList.ZigZag5.ZigZagMap[nZigZagIndex] != 0)
            //        {
            //            dPossibleStopLoss = this.CandlesList.ZigZag5.ZigZagMap[nZigZagIndex] - 4 * this.PipsUnit;
            //            nIndex = nZigZagIndex;
            //            break;
            //        }
            //    }

            //    if ((this.StopLoss < dPossibleStopLoss) && (this.CandlesList.Candles[this.CandlesList.CountDec - 1].R_Low > dPossibleStopLoss) && (this.OffLineCandleIndex - this.OffLineBuyIndex > this.CandlesList.ZigZag5.Length - nIndex))
            //    {
            //        //this.StopLoss = dPossibleStopLoss;
            //        //File.AppendAllText(string.Format("C:\\Users\\Or\\Projects\\MBTrading - Graph\\WindowsFormsApplication1\\bin\\x64\\Debug\\b\\o{1}.txt", Consts.FilesPath, this.Symbol.Remove(3, 1)),
            //        //    string.Format("4;{0};{1};{2}\n", this.Symbol, this.StopLoss, this.OffLineCandleIndex));
            //        //File.AppendAllText(string.Format("C:\\Users\\Or\\Projects\\MBTrading - Graph\\WindowsFormsApplication1\\bin\\x64\\Debug\\b\\o{1}.txt", Consts.FilesPath, this.Symbol.Remove(3, 1)),
            //        //    string.Format("4;{0};{1};{2}\n", this.Symbol, this.StopLoss, this.OffLineCandleIndex - (this.CandlesList.ZigZag5.Length - nIndex)));
            //    }
            //}
        }
    }
}