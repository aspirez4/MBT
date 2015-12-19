using System;
using System.Diagnostics;
using System.Threading;
using MongoDB.Driver;
using MBTrading.Utils;

namespace MBTrading
{
    class UI
    {
        private static string strMBTradingTitle = "**************************************************************************\n**************************************************************************\n\n                                 MBTrading\n\n**************************************************************************\n**************************************************************************\n\n\n";
        private static string strCurrShare = string.Empty;
        private static int nState = 1;
        private static int nRefreshUIInterval;
        public static void MainUI()
        {
            new Thread(PrintToScreen).Start();
            string strInput = string.Empty;
            string strSelectedSymbol = string.Empty;

            while (Program.IsProgramAlive)
            {
                strInput = Console.ReadLine();
                
                if (strInput.Length == 2)
                {
                    strSelectedSymbol = string.Empty;

                    foreach (string strCurrSymbol in Program.SymbolsNamesList.Keys)
                    {
                        if ((strCurrSymbol[0] == strInput[0]) && (strCurrSymbol[4] == strInput[1]))
                        {
                            UI.strCurrShare = strCurrSymbol;
                            break;
                        }
                    }

                    UI.nState = 2;
                    Console.Clear();
                    UI.ShareDetails();
                }
                else if (strInput.ToUpper().Equals("1"))
                {
                    UI.nState = 1;
                    Console.Clear();
                    UI.MainTableShares();
                }
            }
        }
        public static void PrintToScreen()
        {
            UI.nRefreshUIInterval = 5000;
            if (Consts.WorkOffLineMode)
                UI.nRefreshUIInterval = 500;

            while (Program.IsProgramAlive)
            {
                Console.Clear();
                switch (UI.nState)
                {
                    case 1:
                    {
                        UI.MainTableShares();
                        break;
                    }
                    case 2:
                    {
                        UI.ShareDetails();
                        break;
                    }
                }

                Thread.Sleep(UI.nRefreshUIInterval);
            }
        }
        public static void MainTableShares()
        {
            string strLastCandles = "\n\n\n\nLast Candles:\n";
            string strTechDetails = "\n\nTechnical Details\n-----------------\nEST                             : {0}\nMBTrading QuouteAPI Last HB     : {1}\nMBTrading FixGW     Last HB     : {2}\nMBTrading LogonIndicator 	: {3}\nMBTrading ServerSeq             : {4}\nCandles                  	: {5}\nThreads                 	: {6}\nMonths                  	: {7}\n";
            strTechDetails = string.Format(strTechDetails,
                                           Time.EST,
                                           QuoteUtils.LastHB,
                                           FixGatewayUtils.LastHB,
                                           FixGatewayUtils.LogonIndicator,
                                           FixGatewayUtils.LastGatewaySequence,
                                           Consts.MINUTE_CANDLES_PRIMARY,
                                           Process.GetCurrentProcess().Threads.Count,
                                           Program.SharesList["AUD/JPY"].OffLineCandleIndex * (Consts.MINUTE_CANDLES_PRIMARY / 60.0) / 480.0);


            string strTableTitle = "\n\n\n\nForex Table\n-----------\nSymbol              Price           EMA           BuyOrder     Position     BuyPrice     StopPrice     Quantity     CurrP&L       TotalP&L         Comm\n\n";
            string strShares = string.Empty;
            double dTotalProfitSum = 0;
            double dTotalLossSum = 0;
            double dTotalPLSum = 0;
            double dTotalCommSum = 0;
            double dCurrPLSum = 0;
            
            foreach (Share sCurrShare in Program.SharesList.Values)
            {
                sCurrShare.CurrPL = FixGatewayUtils.CalculateProfit(sCurrShare.BuyPrice, sCurrShare.CandlesList.CurrPrice, sCurrShare.Symbol, sCurrShare.PositionQuantity);
                dCurrPLSum += sCurrShare.CurrPL;
                dTotalProfitSum += sCurrShare.TotalProfit;
                dTotalLossSum += sCurrShare.TotalLoss;    
                dTotalPLSum += sCurrShare.TotalPL;
                dTotalCommSum += sCurrShare.Commission;

                strShares += string.Format("{0}         {1}      {2}          {3}             {4}     {5}    {6}    {7}   {8}     {9}    {10}\n",
                                           sCurrShare.Symbol,
                                           string.Format("{0,10:0.00000}", sCurrShare.CandlesList.CurrPrice),
                                           string.Format("{0,10:0.00000}", sCurrShare.CandlesList.EMA.Value),
                                           Consts.WorkOffLineMode ? (sCurrShare.PricesQueue.Count.ToString())  :  (sCurrShare.BuyOrder == null ? " " : "T"),
                                           Consts.WorkOffLineMode ? (sCurrShare.OffLineCandleIndex.ToString()) :  (sCurrShare.IsPosition ? string.Format("T{0}", sCurrShare.StopLossOrders.Count) : "  "),
                                           sCurrShare.BuyPrice == 0 ? "          " : string.Format("{0,10:0.00000}", sCurrShare.BuyPrice),
                                           sCurrShare.StopLoss == 0 ? "          " : string.Format("{0,10:0.00000}", sCurrShare.StopLoss),
                                           sCurrShare.IsPosition ? string.Format("{0,7:0}", sCurrShare.PositionQuantity) : "       ",
                                           string.Format("{0,10:0.0}", sCurrShare.CurrPL),
                                           string.Format("{0,10:0.0}", sCurrShare.TotalPL),
                                           string.Format("{0,10:0.00}", sCurrShare.Commission));

            }

            Console.WriteLine(string.Format("{0}{1}{2}{3}\n\n\n----------------------------------\nCurr  PL   :   {4}\n----------------------------------\nTotal Comm :   -{5}\nTotal PL   :   {6}\nTotal      :   {7}\n----------------------------------{8}", UI.strMBTradingTitle, strTechDetails, strTableTitle, strShares, dCurrPLSum, dTotalCommSum, dTotalPLSum, dTotalPLSum - dTotalCommSum, strLastCandles));
            Program.AccountBallance = Consts.QUANTITY + dTotalPLSum - dTotalCommSum;
            PushServer.SendTCPMessage(PushServer.RealtimeMessage(Program.AccountBallance, dTotalProfitSum, dTotalLossSum, Program.SharesList));
            PushServer.SendTCPMessage("2");
        }
        public static void ShareDetails()
        {
            if (Program.SharesList.ContainsKey(UI.strCurrShare))
            {
                Share sCurrShare = Program.SharesList[UI.strCurrShare];
                string strPointsArray = "-----------------------\n#        High         Low\n";

                string strShareDetails = string.Format("{0} Details\n---------------\n\nGradient         :       {1}\n10Avg            :            {2}\n20Avg            :            {3}\nDownwardLow      :       {4}\nPrice            :       {5}\n\n\n\nIsPosition       :            {6}\nQuantity         :       {7}\nBuy  orders      :       {8}\nStop orders      :       {9}\nStopLoss price   :       {10}\nBuy price        :       {11}\n\n\n\nHigh Direction   : {12}\nLow  Direction   : {13}\nQueue            : {14}\n{15}",
                                                       sCurrShare.Symbol,
                                                       string.Format("{0,6:0.00}", 0),
                                                       sCurrShare.CandlesList.SMA.Value - sCurrShare.CandlesList.SMA.Prev > 0 ? "+" : " ",
                                                       sCurrShare.CandlesList.SMA.Value - sCurrShare.CandlesList.SMA.Prev > 0 ? "+" : " ",
                                                       string.Format("{0,6:0.00000}", 0),
                                                       string.Format("{0,6:0.00000}", sCurrShare.CandlesList.CurrPrice),
                                                       sCurrShare.IsPosition ? "T" : "F",
                                                       sCurrShare.IsPosition ? string.Format("{0,6:0}",sCurrShare.PositionQuantity) : " ",
                                                       sCurrShare.BuyOrder == null ? "" : sCurrShare.BuyOrder.ClientOrdID,
                                                       sCurrShare.StopLossOrders.Count == 0 ? "" : "Has StopLossOrder - " + sCurrShare.StopLossOrders.Count,
                                                       sCurrShare.StopLoss == 0 ? "" : string.Format("{0,6:0.00000}", sCurrShare.StopLoss),
                                                       sCurrShare.BuyPrice == 0 ? "" : string.Format("{0,6:0.00000}", sCurrShare.BuyPrice),
                                                       0 > 0 ? "+" : "-",
                                                       0 > 0 ? "+" : "-",
                                                       sCurrShare.PricesQueue.Count,
                                                       0 == 0 ? "" : strPointsArray);
                
                string strCandles = "\n\n\n\nAll Candles\n-----------\n";
                foreach (Candle cCurr in sCurrShare.CandlesList.Candles)
                {
                    strCandles += string.Format("{0} -  C:{1} O:{2} H:{3} L:{4}\n", cCurr.StartDate, 
                                                                                    string.Format("{0,6:0.00000}", cCurr.Close),
                                                                                    string.Format("{0,6:0.00000}", cCurr.Open),
                                                                                    string.Format("{0,6:0.00000}", cCurr.High),
                                                                                    string.Format("{0,6:0.00000}", cCurr.Low));
                }
                Console.WriteLine(string.Format("{0}{1}{2}", UI.strMBTradingTitle, strShareDetails, strCandles));
            }
            else
            {
                UI.MainTableShares();
            }
        }
    }
}
