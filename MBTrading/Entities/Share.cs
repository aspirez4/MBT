using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MBTrading.Utils;
using MBTrading.Entities;

namespace MBTrading
{
    public class Share
    {
        public bool NNActive = false;
        public int D_MilitraizedZone = 10; // 10
        public int StrategyTriger = 4;    // 4
        public double Risk = 0;
        public bool PartialMode = false;
		public bool bWasPartiald = false;
		
        public NNOrdersList             NNOrdersList;
        public int                      nNeuralNetworkLearnCounter          = Consts.NEURAL_NETWORK_CONST_CHANK_BETWEEN_NN_LEARNING;
        public bool                     bDidFirstConditionHappened          = false;
        public bool                     bDidSecondConditionHappened         = false;
        public double                   StrongMinLow;
        public int                      CandleAfterStrongMinIndex           = 0;
        public int                      StartReversalIndex                  = 0;
        public ChangingPriceForOrder    ChangingStopPirce                   = null;
        public double                   ReversalStopLimitPrice              = 0;
        public bool                     CrossIndicator                      = false;


        public CandlesList                          CandlesList;
        public ConcurrentQueue<MarketData>          PricesQueue;
        public Order                                BuyOrder;
        public double                               TotalPL;
        public double                               TotalProfit;
        public double                               TotalLoss;
        public double                               CurrPL;
        public double                               Commission;
        public string                               Symbol;
        public bool                                 IsPosition;
        public bool                                 IsBuyOrderSentOrExecuted;
        public double                               BuyPrice;
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
            this.CurrPL                     = 0;
            this.TotalPL                    = 0;
            this.TotalProfit                = 0;
            this.TotalLoss                  = 0;
            this.Commission                 = 0;
            this.PositionQuantity           = 0;
            this.BuyPrice                   = 0;
            this.SellPrice                  = 0;
            this.StopLoss                   = 0;
            this.StopLossOrders             = new ConcurrentDictionary<string, Order>();
            this.IsPosition                 = false;
            this.IsBuyOrderSentOrExecuted   = false;
            this.Symbol                     = strSymbol;
            this.PositionsReport            = false;
            this.NNOrdersList               = new NNOrdersList();
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
            this.BuyPrice = 0;
            this.StopLoss = 0;
            this.CurrPL = 0;
            this.PositionsReport = false;

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
                                                                  Consts.QUANTITY,
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
                if ((cPreviousCandle.Low < this.CandlesList.SMA_LowerBollinger) &&
                    (cPreviousCandle.Open - cPreviousCandle.Close) > this.CandlesList.CandleSizeAVG)
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
                    (cPreviousCandle.Low < this.CandlesList.SMA_LowerBollinger) && 
                    (cPreviousCandle.Close > this.CandlesList.SMA_LowerBollinger) && 
                    (cPreviousCandle.Open > this.CandlesList.SMA_LowerBollinger) && 
                    (cPreviousCandle.Close > cPreviousCandle.Open) && 
                    (cPreviousCandle.StartWMA < this.CandlesList.WMA))
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
                                                                                     Consts.QUANTITY,
                                                                                     "MyBollingerStrategy",
                                                                                     this.CandleIndex + 1);
                }
                #endregion

                #region Update Stoploss
                // This section waites from the moment that MA's cross each other, to the moment that WMA direction is changing UP again but still bellow the EMA
                // Potential Sell conditions - MA's crossed - WMA bellow EMA - Starting of a Downward
                bool bCrossMA = ((this.CandlesList.PrevCandle.StartWMA - this.CandlesList.PrevCandle.StartEMA > (this.PipsUnit * 3)) && (this.CandlesList.WMA - this.CandlesList.EMA <= (this.PipsUnit * 3)));

                // Set the indicator to True wen => Cross occurd || already set it befor to True
                this.CrossIndicator = (((bCrossMA) && (this.IsPosition)) || (this.CrossIndicator));

                // If cross occourd (Both on STRATEGY position or in REVERSAL position), update the stoploss in chance of little reversal
                if ((this.IsPosition) && (this.CrossIndicator) && (!cBeforePreviousCandle.WMADirection) && (cPreviousCandle.WMADirection) && (this.CandlesList.EMA > this.CandlesList.WMA))
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
                                                                                             Consts.QUANTITY, 
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




        public double CalcStopLoss()
        {
            double dStopLoss = double.MaxValue;

            bool bWMADir = this.CandlesList.Candles[this.CandlesList.CountDec].WMADirection;
            for (int nWMADirInex = this.CandlesList.Candles.Count - 2; nWMADirInex > 0 /* this.CandlesList.CountDec - 10 */ ; nWMADirInex--)
            {
                if ((bWMADir) && !(this.CandlesList.Candles[nWMADirInex].WMADirection) && dStopLoss > this.CandlesList.Candles[nWMADirInex].Low)
                { dStopLoss = this.CandlesList.Candles[nWMADirInex].Low; /* break; */ }
                bWMADir = this.CandlesList.Candles[nWMADirInex].WMADirection;
            }

            return (dStopLoss - 50 * this.PipsUnit);
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
                this.nNeuralNetworkLearnCounter--;
                this.CandlesList.NeuralNetworSelfAwarenessPredict(0.5);
                this.PrintOutPrediction();

                if (this.OffLineCandleIndex > 25)
                {
                    double dStop = this.CalcStopLoss();

                    NNOrder n = new NNOrder(this,
                                    dStop,
                                    this.CandlesList.CurrPrice,
                                    this.CandlesList.Candles,
                                    true);
                    this.NNOrdersList.Add(n);

                }
            }

            this.NNOrdersList.CheckOrders(this.CandlesList.CurrPrice, bIsNewCandle);
            if ((this.nNeuralNetworkLearnCounter <= 0) && (this.NNActive) && (this.CandlesList.NeuralNetworkSelfAwarenessCollection.Count > 5))
            {
                nNeuralNetworkLearnCounter = Consts.NEURAL_NETWORK_CONST_CHANK_BETWEEN_NN_LEARNING;
                this.CandlesList.NeuralNetworkSelfAwarenessActivate();
            }
            #endregion

            #region Long!
            if (bIsNewCandle)
            {
                Candle cMegaPreviousCandle = this.CandlesList.Candles[this.CandlesList.CountDec - 3];
                Candle cBeforePreviousCandle = this.CandlesList.Candles[this.CandlesList.CountDec - 2];
                Candle cPreviousCandle = this.CandlesList.Candles[this.CandlesList.CountDec - 1];

                if ((cPreviousCandle.EndTDI_Green > cPreviousCandle.EndTDI_Red) && 
                    (cBeforePreviousCandle.EndTDI_Green < cBeforePreviousCandle.EndTDI_Red) &&
                    (cPreviousCandle.EndTDI_Green < cPreviousCandle.EndTDI_Mid))
                {
                    if (!this.OffLineIsPosition)
                    {
                        this.OffLineBuy(this.CalcStopLoss(), false);
                    }
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
                bool bCrossMA = ((this.CandlesList.PrevCandle.StartWMA - this.CandlesList.PrevCandle.StartEMA > this.PipsUnit * this.D_MilitraizedZone) && (this.CandlesList.WMA - this.CandlesList.EMA <= this.PipsUnit * this.D_MilitraizedZone));

                // Set the indicator to True wen => Cross occurd || already set it befor to True
                this.CrossIndicator = (((bCrossMA) && (this.OffLineIsPosition)) || (this.CrossIndicator));

                if (!cBeforePreviousCandle.WMADirection && cPreviousCandle.WMADirection && this.CrossIndicator && this.OffLineIsPosition && this.CandlesList.EMA > this.CandlesList.WMA)
                {
                    this.StopLoss = Math.Min(cBeforePreviousCandle.Low, cPreviousCandle.Low) - (this.PipsUnit * 2);
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
        }
        public void OffLineBuy(double dStopLoss, bool bStrategy)
        {
            if ((!this.NNActive) || (this.CandlesList.NNStrategy == null) ||
                ((this.CandlesList.NNStrategy.AccuracyRate > 0.2) && 
                 ((this.CandlesList.Candles[this.CandlesList.CountDec - 1].ProfitPredictionStrategy > 0.2) || 
                  (this.CandlesList.Candles[this.CandlesList.CountDec - 2].ProfitPredictionStrategy > 0.2))))
  //              ||
  //              (!bStrategy && (this.CandlesList.NNOther.AccuracyRate > 0.6) &&
  //               ((this.CandlesList.Candles[this.CandlesList.CountDec - 1].ProfitPredictionOther > 0.5) ||
  //                (this.CandlesList.Candles[this.CandlesList.CountDec - 2].ProfitPredictionOther > 0.5))))
            {
                // BUYYYYYYYYYYY
                this.StartReversalIndex = 0;
                this.OffLineBuyIndex = this.OffLineCandleIndex;

                this.BuyPrice = this.CandlesList.LastCandle.Bid;

                this.OffLineIsPosition = true;
                this.StopLoss = 0.01;// this.BuyPrice - this.PipsToStopLoss;
                this.StopLoss = dStopLoss;
				
				this.Risk = this.BuyPrice - this.StopLoss;
				
                this.PositionQuantity = Consts.QUANTITY;
                this.Commission += FixGatewayUtils.CalculateCommission(this.CandlesList.CurrPrice, this.Symbol, this.PositionQuantity);
                File.AppendAllText(string.Format("C:\\Users\\Or\\Projects\\Graph\\WindowsFormsApplication1\\bin\\Debug\\b\\o{1}.txt", Consts.FilesPath, this.Symbol.Remove(3, 1)),
                    string.Format("1;{0};{1};{2}\n", this.Symbol, this.BuyPrice, this.OffLineCandleIndex));


                if (true)
                {
                    this.NNOrdersList.Add(new NNOrder(this,
                        dStopLoss,
                        this.BuyPrice,
                        this.CandlesList.Candles,
                        bStrategy));
                }
            }
            else
            {
                if (OffLineVirtualBuyIndex != this.OffLineCandleIndex)
                {
                    this.OffLineVirtualBuyIndex = this.OffLineCandleIndex;
                    this.StopLoss = dStopLoss;
                    this.PrintOutPrediction();

                    if (true)
                    {
                        this.NNOrdersList.Add(new NNOrder(this,
                            dStopLoss,
                            this.CandlesList.CurrPrice + this.PipsAboveForLimitPrice,
                            this.CandlesList.Candles,
                            bStrategy));
                    }
                }
            }
        }
        public void OffLineSell(bool bLong)
        {
            // SELLLLLLLLLL
            this.UpdateShareConsts();
            this.SellPrice = this.CandlesList.CurrPrice;
            this.OffLineSellIndex = this.OffLineCandleIndex;
            this.CrossIndicator = false;
            this.OffLineIsPosition = false;
			this.bWasPartiald = false;
            this.Commission += FixGatewayUtils.CalculateCommission(this.CandlesList.CurrPrice, this.Symbol, this.PositionQuantity);
            double dPL = FixGatewayUtils.CalculateProfit(this.BuyPrice, this.CandlesList.CurrPrice, this.Symbol, this.PositionQuantity);
            MongoDBUtils.DBEventAfterPositionSell(Program.AccountBallance, this.Symbol, dPL, this.OffLineCandleIndex, this.OffLineCandleIndex - this.OffLineBuyIndex, this.BuyPrice, this.CandlesList.CurrPrice);
            this.TotalPL += dPL;
            if (dPL < 0)
            { this.TotalLoss += dPL; }
            else
            { this.TotalProfit += dPL; }
            this.BuyPrice = 0;
            this.StopLoss = 0;
            this.PositionQuantity = 0;
            File.AppendAllText(string.Format("C:\\Users\\Or\\Projects\\Graph\\WindowsFormsApplication1\\bin\\Debug\\b\\o{1}.txt", Consts.FilesPath, this.Symbol.Remove(3, 1)),
                string.Format("0;{0};{1};{2}\n", this.Symbol, this.CandlesList.CurrPrice, this.OffLineCandleIndex));
        }
		public void OffLinePartialSell(bool bLong, double dPartialPrcentage)
        {
            // SELLLLLLLLLL
            this.UpdateShareConsts();
			int dSoldCount = (int)(this.PositionQuantity * dPartialPrcentage);

            this.Commission += FixGatewayUtils.CalculateCommission(this.CandlesList.CurrPrice, this.Symbol, dSoldCount);
            double dPL = FixGatewayUtils.CalculateProfit(this.BuyPrice, this.CandlesList.CurrPrice, this.Symbol, dSoldCount);
            MongoDBUtils.DBEventAfterPositionSell(Program.AccountBallance, this.Symbol, dPL, this.OffLineCandleIndex, this.OffLineCandleIndex - this.OffLineBuyIndex, this.BuyPrice, this.CandlesList.CurrPrice);
            this.TotalPL += dPL;
            if (dPL < 0)
            { this.TotalLoss += dPL; }
            else
            { this.TotalProfit += dPL; }
			this.bWasPartiald = true;
            this.PositionQuantity -= dSoldCount;
            File.AppendAllText(string.Format("C:\\Users\\Or\\Projects\\Graph\\WindowsFormsApplication1\\bin\\Debug\\b\\o{1}.txt", Consts.FilesPath, this.Symbol.Remove(3, 1)),
                string.Format("0;{0};{1};{2}\n", this.Symbol, this.CandlesList.CurrPrice, this.OffLineCandleIndex));
        }
        public void PrintOutPrediction()
        {
            if (this.CandlesList.NNStrategy != null)
            {
                string strLable1 = this.CandlesList.NNStrategy.AccuracyRate.ToString() + " : " + this.CandlesList.Candles[this.CandlesList.CountDec - 2].ProfitPredictionStrategy + " > " + this.CandlesList.Candles[this.CandlesList.CountDec - 1].ProfitPredictionStrategy;
                string strLable2 = this.CandlesList.Candles[this.CandlesList.CountDec - 1].ProfitPredictionStrategy.ToString();

                File.AppendAllText(string.Format("C:\\Users\\Or\\Projects\\Graph\\WindowsFormsApplication1\\bin\\Debug\\b\\o{1}.txt", Consts.FilesPath, this.Symbol.Remove(3, 1)),
                    string.Format("2;{0};{1};{2}\n", this.Symbol, strLable2, this.OffLineCandleIndex));
            }
        }
    }
}