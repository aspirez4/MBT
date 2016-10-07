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
        public static int NUMOFTRADES = 0;
        public static int NUMOFP = 0;
        public static int NUMOFL = 0;

        public bool NNActive = false;
        public int D_MilitraizedZone = 10; // 10
        public double Risk = 0;
        public bool PartialMode = false;
        public bool bWasPartiald = false;
        private List<double> tempList;
        private List<double> tempListNN;
        public List<Pattern> TempPatterns;
        public List<Pattern> Patterns;
        public int nForcedSpread = 0;
        public int nNumOfTicks = 0;
        public static int nWantedPipsOutcomeProfit = 1;
        public int nWhenToSellIndex = 0;
        public double lastBid = 0;
        public double lastAsk = 0;
        public long tickIndex = 0;
        public long inc()
        {
            long l = tickIndex++;
            if (this.tickIndex == long.MaxValue)
                this.tickIndex = 0;
            return l;
        }


        public bool bDidFirstConditionHappened = false;
        public bool bDidSecondConditionHappened = false;
        public double StrongMinLow;
        public int CandleAfterStrongMinIndex = 0;
        public int StartReversalIndex = 0;
        public double ReversalStopLimitPrice = 0;
        public bool CrossIndicator = false;
        public bool CrossEMALine = false;


        public CandlesList CandlesList;
        public ConcurrentQueue<MarketData> PricesQueue;
        public Order BuyOrder;
        public double TotalPL;
        public double TotalPipsPL;
        public double TotalProfit;
        public double TotalLoss;
        public double CurrPL;
        public double Commission;
        public string Symbol;
        public bool IsPosition;
        public bool IsLong;
        public Dictionary<long, Pair> BuyPrices;
        public double AverageBuyPrice;
        public double SellPrice;
        public double StopLoss;
        public int PositionQuantity;
        public bool PositionsReport;
        public ConcurrentDictionary<string, Order> StopLossOrders;

        public double PipsUnit;
        public double PipsAboveForLimitPrice;
        public double PipsToStopLimit;
        public double PipsToStopLoss;
        public double PipsToPartial;
        public int CandleIndex = 0;
        public int BuyIndex = -1;
        public int BuyDirection = -1;
        public double FirstStopLoss = -1;
        public int SellIndex = -2;

        public bool OffLineIsPosition = false;
        public bool OffLineOrBolBuy = false;
        public int OffLineCandleIndex = 0;
        public int OffLineBuyIndex = 0;
        public int OffLineSellIndex = 0;
        public int OffLineSignalIndex = 0;
        public int OffLineVirtualBuyIndex = 0;

        // MachineLearning params
        public int PatternLength;
        public int PatternOutcomeIntervel;
        public int PatternOutcomeRange;

        // CTors
        static Share() { }
        public Share(string strSymbol)
        {
            this.PricesQueue = new ConcurrentQueue<MarketData>();
            this.BuyOrder = null;
            this.TotalPipsPL = 0;
            this.CurrPL = 0;
            this.TotalPL = 0;
            this.TotalProfit = 0;
            this.TotalLoss = 0;
            this.Commission = 0;
            this.PositionQuantity = 0;
            this.BuyPrices = new Dictionary<long, Pair>();
            this.AverageBuyPrice = 0;
            this.SellPrice = 0;
            this.StopLoss = 0;
            this.StopLossOrders = new ConcurrentDictionary<string, Order>();
            this.IsPosition = false;
            this.Symbol = strSymbol;
            this.PositionsReport = false;
            this.tempList = new List<double>();
            this.tempListNN = new List<double>();
            this.TempPatterns = Pattern.AllPatterns[this.Symbol];
            for (int i = 0; i < Consts.NEURAL_NETWORK_MA_LENGTH; i++) { this.tempList.Add(0); }
            for (int i = 0; i < Consts.NEURAL_NETWORK_MA_LENGTH; i++) { this.tempListNN.Add(0); }
        }
        public void UpdateShareConsts()
        {
            PipsUnit = 0.0001;
            PipsAboveForLimitPrice = 0.0001 * Consts.PIPS_ABOVE_FOR_LIMIT_PRICE;
            PipsToStopLimit = 0.0001 * Consts.PIPS_TO_STOP_LIMIT;
            PipsToStopLoss = 0.0001 * Consts.PIPS_TO_STOP_LOSS;
            PipsToPartial = 0.0001 * Consts.PIPS_TO_PARTIAL;

            if (this.Symbol.Contains("JPY"))
            {
                PipsUnit = 0.01;
                PipsAboveForLimitPrice = 0.01 * Consts.JPY_PIPS_ABOVE_FOR_LIMIT_PRICE;
                PipsToStopLimit = 0.01 * Consts.JPY_PIPS_TO_STOP_LIMIT;
                PipsToStopLoss = 0.01 * Consts.JPY_PIPS_TO_STOP_LOSS;
                PipsToPartial = 0.01 * Consts.JPY_PIPS_TO_PARTIAL;
            }
        }
        public void InitializeShere()
        {
            Loger.ExecutionReport(this.Symbol, null, false, "Initilizing");
            new Thread(InitializeProcedure).Start();
        }
        private void InitializeProcedure()
        {
            this.CancelAllStopLossOrders();
            this.UpdateShareConsts();

            this.flag = false;
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
            Loger.ExecutionReport(this.Symbol, null, false, "Initilized");
        }



        // Online Mode
        public void Activate()
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
            #region Update MarketData
            inc();
            switch (mdCurrMarketData.DataType)
            {
                case (MarketDataType.Bid):
                    {
                        this.lastBid = mdCurrMarketData.Value;
                        break;
                    }
                case (MarketDataType.Ask):
                    {
                        this.lastAsk = mdCurrMarketData.Value;
                        break;
                    }
            }
            #endregion

            #region Patterns process
            if ((this.TempPatterns.Count == 0) || (this.TempPatterns.Count % 2500 != 0))
            {
                Pattern.Tick(mdCurrMarketData, this.lastBid, this.lastAsk, ref this.TempPatterns, this.PipsUnit);
            }
            else
            {
                if (this.Patterns != null && this.Patterns.Count > 0)
                    Pattern.claen(this.Patterns, 0, this.Patterns.Count - 1);

                this.Patterns = Pattern.BuildPatterns(this.Symbol);
                this.TempPatterns = Pattern.AllPatterns[this.Symbol];
            }
            #endregion


            #region Buy and Sell
            if (!this.OffLineIsPosition)
            {
                if (this.Patterns != null && this.Patterns.Count > 0)
                {
                    if (this.TempPatterns.Count > 1 && this.TempPatterns.Last().Dimensions[0].Count == 0)
                    {
                        foreach (Pattern currTodaysPatterns in this.Patterns)
                        {
                            if (currTodaysPatterns.Similar(this.TempPatterns[this.TempPatterns.Count - 2]))
                            {
                                int nDirection = currTodaysPatterns.OutcomeSimPatterns > 0 ? 1 : -1;
                                this.BuyLimitPlusStopMarket(true, //this.Patterns.First().OutcomeSimPatterns > 0,
                                                            nDirection == 1 ? this.lastAsk + this.PipsUnit * 3 : this.lastBid - this.PipsUnit * 3,
                                                            nDirection == 1 ? this.lastBid - 10 * this.PipsUnit : this.lastAsk + 10 * this.PipsUnit,
                                                            Program.Quantity,
                                                            "Long: " + (nDirection > 0).ToString(),
                                                            null);

                                this.nWhenToSellIndex = (Pattern.NumOfTicsSamples * Pattern.OutcomeInterval);
                                File.AppendAllText(string.Format("C:\\temp\\Or\\o{0}.txt", this.Symbol.Remove(3, 1)), string.Format("{0}", this.tickIndex));

                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                this.nNumOfTicks++;
                if ((this.nNumOfTicks == this.nWhenToSellIndex) || ((this.lastBid - this.StopLoss) * this.BuyDirection < 0))
                {
                    this.SellMarket();
                    File.AppendAllText(string.Format("C:\\temp\\Or\\o{0}.txt", this.Symbol.Remove(3, 1)), string.Format("   {0}     profit: {1}\n", this.tickIndex, (this.BuyDirection * (this.SellPrice - this.AverageBuyPrice)) / this.PipsUnit));
                }
            }
            #endregion
        }


        public bool BuyLimitPlusStopMarket(bool bIsLong,
                                           double dLimit,
                                           double dStopLoss,
                                           int nQuantity,
                                           string strLogMessage,
                                           int? nCandleIndexTTL)
        {
            //Order oBuy = new Order(this.Symbol, nQuantity, dLimit, null, dStopLoss, true, bIsLong, nCandleIndexTTL);
            //return (FixGatewayUtils.Buy(oBuy));

            if (OandaGatewayUtils.Buy(this.Symbol, nQuantity, bIsLong, dLimit, dStopLoss))
            {
                this.IsPosition = true;
                this.PositionQuantity = nQuantity;
                this.IsLong = bIsLong;
                this.StopLoss = dStopLoss;
                this.BuyPrices.Add(this.tickIndex, new Pair(nQuantity, dLimit));
                this.AverageBuyPrice = dLimit;
                this.Commission += FixGatewayUtils.CalculateCommission(this.lastAsk, this.Symbol, this.PositionQuantity, bIsLong, true);
                return true;
            }

            return false;
        }
        public bool SellMarket()
        {
            //return (FixGatewayUtils.Sell(new Order(this.Symbol, this.PositionQuantity, null, null, null, false, false, null)));
            if (OandaGatewayUtils.Sell(this.Symbol, this.PositionQuantity, this.IsLong))
            {
                this.InitializeProcedure();
                this.IsPosition = true;
                this.PositionQuantity = 0;
                return true;
            }

            return false;
        }
        public void SellPartial(int nQuantityForPartial, bool bUpdateStopLossAlso)
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
        public void UpdateAllStopLossOrders(double? dStop, int? nQuantity)
        {
            double dPrevStopLoss = this.StopLoss;

            foreach (Order oStopOrder in this.StopLossOrders.Values)
            {
                oStopOrder.UpdateStopLossOrder_Async(dStop, nQuantity);
            }

            double dPipsChange = dStop != null ? this.StopLoss - dPrevStopLoss : 0;

            Loger.ExecutionReport(this.Symbol, null, false, string.Format("Updating all stopLosses ({0}) - {1}Pips", this.StopLossOrders.Count, this.PipsUnit * dPipsChange));
        }
        public void CancelAllStopLossOrders()
        {
            Loger.ExecutionReport(this.Symbol, null, false, string.Format("Canceling all stopLosses ({0})", this.StopLossOrders.Count));
            foreach (Order oStopOrder in this.StopLossOrders.Values)
            {
                oStopOrder.CancelOrder_Async(null);
            }
        }




        // Didnt SCOD it!!!
        public bool StopSell(double dStop)
        {
            if (FixGatewayUtils.LogonIndicator)
            {
                FixGatewayUtils.Sell(new Order(this.Symbol, this.PositionQuantity, null, dStop, null, false, false, null));
                return (true);
            }

            return (false);
        }
        // מרגע שליחת פקודת הקניה...כל 60 שניות, צריך לבדוק אם מחיר מאיזושהי סיבה, המחיר הנוכחי ירד מתחת למחיר הסטופ והמניה עדיין פוזיציה
        public void SecurePosition()
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
        public static void PositionStatus()
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
            inc();
            switch (mdCurrMarketData.DataType)
            {
                case (MarketDataType.Bid):
                    {
                        this.lastBid = mdCurrMarketData.Value;
                        break;
                    }
                case (MarketDataType.Ask):
                    {
                        this.lastAsk = mdCurrMarketData.Value;
                        break;
                    }
            }



            if ((this.TempPatterns.Count == 0) || (this.TempPatterns.Count % 2500 != 0))
            {
                Pattern.Tick(mdCurrMarketData, this.lastBid, this.lastAsk, ref this.TempPatterns, this.PipsUnit);
            }
            else
            {
                if (this.Patterns != null && this.Patterns.Count > 0)
                    Pattern.claen(this.Patterns, 0, this.Patterns.Count - 1);

                this.Patterns = Pattern.BuildPatterns(this.Symbol);
                this.TempPatterns = Pattern.AllPatterns[this.Symbol];

                #region

                if (Patterns.Count > 0)
                {
                    if (Patterns[0].OutcomeSimPatterns * Patterns[0].OutcomeSimPatterns > nWantedPipsOutcomeProfit)
                    {
                        File.AppendAllText(string.Format("C:\\temp\\Or\\p{0}.txt", this.Symbol.Remove(3, 1)), string.Format("config: Tics: {0}    Interval: {1}\n", Pattern.NumOfTicsSamples, Pattern.OutcomeInterval));
                        File.AppendAllText(string.Format("C:\\temp\\Or\\p{0}.txt", this.Symbol.Remove(3, 1)), "-------------------------------------------------\n");
                        File.AppendAllText(string.Format("C:\\temp\\Or\\p{0}.txt", this.Symbol.Remove(3, 1)), string.Format("0: {0}    {1}%    {2}\n", Patterns[0].SimPatterns.Count, (int)Patterns[0].Accuracy, Patterns[0].OutcomeSimPatterns));
                        foreach (Pattern p in Patterns[0].SimPatterns)
                        {
                            File.AppendAllText(string.Format("C:\\temp\\Or\\p{0}.txt", this.Symbol.Remove(3, 1)), string.Format(" - {0}\n", p.OutcomePrivate));
                        }
                        File.AppendAllText(string.Format("C:\\temp\\Or\\p{0}.txt", this.Symbol.Remove(3, 1)), "\n");
                    }
                    else
                    {
                        double dSomeone = 0;
                        double dSomeoneCount = 0;
                        double dSomeoneAccuracy = 0;
                        foreach (Pattern p in Patterns)
                        {
                            if (p.OutcomeSimPatterns * p.OutcomeSimPatterns > nWantedPipsOutcomeProfit)
                            {
                                dSomeone = p.OutcomeSimPatterns;
                                dSomeoneCount = p.SimPatterns.Count;
                                dSomeoneAccuracy = p.Accuracy;
                                break;
                            }
                        }
                        File.AppendAllText(string.Format("C:\\temp\\Or\\p{0}.txt", this.Symbol.Remove(3, 1)), string.Format(" x  {0} -----> {1} ({2} | {3}%)\n", Patterns[0].OutcomeSimPatterns, dSomeone, dSomeoneCount, (int)dSomeoneAccuracy));
                    }
                }
                #endregion
            }



            if (!this.OffLineIsPosition)
            {
                if (this.Patterns != null && this.Patterns.Count > 0)
                {
                    if (this.TempPatterns.Count > 1 && this.TempPatterns.Last().Dimensions[0].Count == 0)
                    {
                        int nTry = 0;
                        foreach (Pattern currTodaysPatterns in this.Patterns)
                        {
                            if (currTodaysPatterns.Similar(this.TempPatterns[this.TempPatterns.Count - 2]))
                            {
                                int nDirection = currTodaysPatterns.OutcomeSimPatterns > 0 ? 1 : -1;
                                this.OffLineBuy(nDirection == 1 ? 
                                    (this.lastBid - 2.2 * this.PipsUnit) : 
                                    (this.lastAsk + 2.2 * this.PipsUnit), 
                                    10000, 
                                    nDirection > 0);
                                this.nWhenToSellIndex = (Pattern.NumOfTicsSamples * Pattern.OutcomeInterval); //+ (new Random().Next(0, 5) * Pattern.NumOfTicsSamples);
                                File.AppendAllText(string.Format("C:\\temp\\Or\\o{0}.txt", this.Symbol.Remove(3, 1)), string.Format("{0}: {1}",nTry++, this.tickIndex));

                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                this.nNumOfTicks++;

                // First time trailing is triggered
                //if ((!this.flag) && (this.nNumOfTicks > this.nWhenToSellIndex) &&
                //    (((this.BuyDirection == 1) && (this.lastBid > this.AverageBuyPrice +  3*this.PipsUnit) ||
                //      (this.BuyDirection == -1) && (this.lastAsk < this.AverageBuyPrice - 3*this.PipsUnit))))
                //{
                //    this.flag = true;
                //    this.StopLoss = this.AverageBuyPrice;
                //}
                //
                //// X trailing
                //if (this.flag && 
                //    ((this.BuyDirection == 1) && (this.lastBid > this.StopLoss +  2*this.PipsUnit) ||
                //     (this.BuyDirection == -1) && (this.lastAsk < this.StopLoss - 2*this.PipsUnit)))
                //{
                //    this.StopLoss = (this.BuyDirection == 1) ? this.lastBid - 2 * this.PipsUnit :
                //                                               this.lastAsk + 2 * this.PipsUnit; 
                //}

                if (((this.BuyDirection == 1) && (this.lastBid > this.AverageBuyPrice + 0.2 * this.PipsUnit) ||
                    (this.BuyDirection == -1) && (this.lastAsk < this.AverageBuyPrice - 0.2 * this.PipsUnit)))// this.nNumOfTicks > this.nWhenToSellIndex && OffLineCheackIsProffit())
                {
                    Interlocked.Increment(ref NUMOFP);
                    int n = this.nNumOfTicks;
                    this.OffLineSell();
                    File.AppendAllText(string.Format("C:\\temp\\Or\\o{0}.txt", this.Symbol.Remove(3, 1)), string.Format("   {0}     profit: {1}             (P:{2} | L:{3})\n", n, (this.BuyDirection * (this.SellPrice - this.AverageBuyPrice)) / this.PipsUnit, (int)NUMOFP * 100 / NUMOFTRADES, (int)NUMOFL * 100 / NUMOFTRADES));
                }
                else if (((this.BuyDirection == 1) && (this.lastBid < this.StopLoss)) ||
                        ((this.BuyDirection == -1) && (this.lastAsk > this.StopLoss))) 
                {
                    Interlocked.Increment(ref NUMOFL);
                    int n = this.nNumOfTicks;
                    bool bbb = this.flag;
                    this.OffLineSell();

                    //if (bbb)
                    //{ File.AppendAllText(string.Format("C:\\temp\\Or\\o{0}.txt", this.Symbol.Remove(3, 1)), string.Format("   {0}     profit: {1}\n", n, (this.BuyDirection * (this.SellPrice - this.AverageBuyPrice)) / this.PipsUnit)); }
                    //else
                    { File.AppendAllText(string.Format("C:\\temp\\Or\\o{0}.txt", this.Symbol.Remove(3, 1)), string.Format("   {0}     !.!.!.: {1}             (P:{2} | L:{3})\n", n, (this.BuyDirection * (this.SellPrice - this.AverageBuyPrice)) / this.PipsUnit, (int)NUMOFP * 100 / NUMOFTRADES, (int)NUMOFL * 100 / NUMOFTRADES)); }
                }
            }
        }

        public bool flag = false;

        public void OffLineBuy(double dStopLoss, int nQuantity, bool bLong)
        {
            Interlocked.Increment(ref NUMOFTRADES);

            // BUYYYYYYYYYYY
            this.BuyDirection = bLong ? 1 : -1;
            this.StartReversalIndex = 0;
            this.OffLineBuyIndex = this.OffLineBuyIndex == 0 ? this.OffLineCandleIndex : this.OffLineBuyIndex;

            this.BuyPrices.Add(this.OffLineCandleIndex, new Pair(nQuantity, bLong ? this.lastAsk : this.lastBid));
            this.AverageBuyPrice = 0;
            foreach (Pair pBuy in this.BuyPrices.Values)
                this.AverageBuyPrice += pBuy.Price;
            this.AverageBuyPrice /= this.BuyPrices.Count;
            this.StopLoss = dStopLoss;



            this.OffLineIsPosition = true;

            this.PositionQuantity += nQuantity;
//            this.Commission += FixGatewayUtils.CalculateCommission(bLong ? this.lastAsk : this.lastBid, this.Symbol, this.PositionQuantity, bLong,true);

            File.AppendAllText(string.Format("C:\\Users\\Or\\Projects\\MBTrading - Graph\\WindowsFormsApplication1\\bin\\x64\\Debug\\b\\o{1}.txt", Consts.FilesPath, this.Symbol.Remove(3, 1)),
                string.Format("1;{0};{1};{2}\n", this.Symbol, bLong ? this.lastAsk : this.lastBid, this.OffLineCandleIndex));
        }
        public void OffLineSell()
        {
            // SELLLLLLLLLL
            this.UpdateShareConsts();
            this.flag = false;
            this.nNumOfTicks = 0;
            this.FirstStopLoss = -1;
            this.SellPrice = (this.BuyDirection == 1 ? this.lastBid : this.lastAsk) - (this.BuyDirection * this.PipsUnit * nForcedSpread);
            this.OffLineSellIndex = this.OffLineCandleIndex;
            this.CrossIndicator = false;
            this.CrossEMALine = false;
            this.OffLineIsPosition = false;
            this.bWasPartiald = false;
            this.OffLineBuyIndex = 0;

//            this.Commission += FixGatewayUtils.CalculateCommission(this.SellPrice, this.Symbol, this.PositionQuantity, this.BuyDirection == 1, false);
            double dPL = 0;
            foreach (Pair pBuy in this.BuyPrices.Values)
            {
                dPL += FixGatewayUtils.CalculateProfit(pBuy.Price, this.SellPrice, this.Symbol, pBuy.Quantity, this.BuyDirection == 1);
                this.TotalPipsPL += (this.BuyDirection * (this.SellPrice - pBuy.Price)) / this.PipsUnit;
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
                string.Format("0;{0};{1};{2};{3}\n", this.Symbol, this.SellPrice, this.BuyDirection, this.OffLineCandleIndex));
        }
        public bool OffLineCheackIsProffit()
        {
            double com = 2 * FixGatewayUtils.CalculateCommission(this.SellPrice, this.Symbol, this.PositionQuantity, this.BuyDirection == 1, false);
            double prof = 0;
            double SellPrice = (this.BuyDirection == 1 ? this.lastBid : this.lastAsk) - (this.BuyDirection * this.PipsUnit * nForcedSpread);
            foreach (Pair pBuy in this.BuyPrices.Values)
            {
                prof += FixGatewayUtils.CalculateProfit(pBuy.Price, SellPrice, this.Symbol, pBuy.Quantity, this.BuyDirection == 1);
            }
            prof = prof * this.BuyDirection;
            return (prof > com);
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