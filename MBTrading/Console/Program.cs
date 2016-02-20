using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MBTrading.Utils;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Diagnostics;

namespace MBTrading
{
    class Program
    {
        public static ConcurrentDictionary<string, Share> SharesList { get; set; }
        public static Dictionary<string, byte[]> SymbolsNamesList { get; set; }
        public static bool IsProgramAlive;
        public static double AccountBallance = Consts.QUANTITY;
        public static int nDayNum = 0;

        public static void Main(string[] args)
        {
            PythonUtils p = new PythonUtils("pyBrain.py", "NN");
            p.CallFunction("InitElman");
        }
        public static void Main1(string[] args)
        {
            Program.IsProgramAlive = true;
  

            // Start WebTrading TCP connection
            new Thread(PushServer.StartListening).Start();


            // Initialize the ExchangeActivity Check and Garbage-Collector work
            new Thread(ManageProgram).Start();


            // Initialize the ExchangeActivity Check and Garbage-Collector work
            new Thread(IncDayNum).Start();


            // Connect to MBT Fix Gateway
            if (!Consts.WorkOffLineMode) { new Thread(FixGatewayUtils.ESTRollover).Start(); }


            // Initialize Shares 
            int nNumOfFails = 0;
            while ((!Program.InitializeShares()) && (nNumOfFails < 3)) { Thread.Sleep(60000); nNumOfFails++; }


            // Activate Shares
            foreach (Share sCurrShare in Program.SharesList.Values) { Thread.Sleep(1000); if (Consts.WorkOffLineMode) { new Thread(sCurrShare.OffLineActivate).Start(); } else { new Thread(sCurrShare.Activate).Start(); } }


            // Get MarketData
            new Thread(QuoteUtils.ConnectMBTQuoteAPI).Start();


            // Starting UI
            UI.MainUI();
        }
        public static void ManageProgram()
        {
            while (Program.IsProgramAlive)
            {
                GC.Collect();
                Thread.Sleep(60000);
            }
        }
        public static void IncDayNum()
        {
            while (true)
            {
                Thread.Sleep(86400000);
                Program.nDayNum++;
            }
        }
        public static bool InitializeShares()
        {
            try
            {
                // Read the symbols
                Program.SymbolsNamesList = Program.GetSymbolsNamesFromFile();
                Program.SharesList = new ConcurrentDictionary<string, Share>();

                // Initialize the shares 
                foreach (string strSymbol in Program.SymbolsNamesList.Keys)
                {
                    Share sNewShare = new Share(strSymbol);
                    sNewShare.UpdateShareConsts();
                    Program.SharesList.TryAdd(strSymbol, sNewShare);

                    if (!Consts.WorkOffLineMode)
                        sNewShare.CandlesList = QuoteUtils_Historical.LoadLast20(
                            sNewShare.Symbol, 
                            Consts.MINUTE_CANDLES_PRIMARY, 
                            true, 
                            DateTime.Now.Subtract(new TimeSpan(20, 0, 0, 0, 0)), 
                            DateTime.Now);
                    else
                        sNewShare.CandlesList = Program.LoadLast20(
                            sNewShare.Symbol, 
                            Consts.MINUTE_CANDLES_PRIMARY, 
                            true, 
                            DateTime.Now.Subtract(new TimeSpan(20, 0, 0, 0, 0)), 
                            DateTime.Now);

                    Console.WriteLine(sNewShare.Symbol + " -  Loaded");
                }
            }
            catch (Exception e)
            {
                Loger.ErrorLog(e);
                return (false);
            }

            return (true);
        }
        public static Dictionary<string, byte[]> GetSymbolsNamesFromFile()
        {
            Dictionary<string, byte[]> dicDictionatyToRet = new Dictionary<string, byte[]>();

            // Read all lines in File
            string[] SymbolsNames = File.ReadAllLines(Consts.SYMBOLS_NAMES_FILE_PATH);

            // Foreach line (symbol)
            foreach (string strCurrSymbol in SymbolsNames)
            {
                dicDictionatyToRet.Add(strCurrSymbol, strCurrSymbol.ToBytesArray());
            }

            return (dicDictionatyToRet);
        }
        public static CandlesList LoadLast20(string strSymbol, int nMinuteBar, bool bIsPrimary, DateTime dtStartTime, DateTime dtEndTime)
        {
            CandlesList cCandleList;
            DateTime dtPreStartTime = dtEndTime.Subtract(new TimeSpan(4, 0, 0, 0, 0));
            Dictionary<string, double> dicInitDictionary = new Dictionary<string, double>();
            dicInitDictionary.Add("USD/JPY", 122.954);
            dicInitDictionary.Add("USD/CHF",0.92895);
            dicInitDictionary.Add("USD/CAD",1.22701);
            dicInitDictionary.Add("NZD/USD",0.68454);
            dicInitDictionary.Add("NZD/JPY",84.634 );
            dicInitDictionary.Add("AUD/USD",0.76852);
            dicInitDictionary.Add("EUR/CHF",1.046  );
            dicInitDictionary.Add("EUR/AUD",1.4571 );
            dicInitDictionary.Add("GBP/USD",1.57001);
            dicInitDictionary.Add("EUR/USD",1.11625);
            dicInitDictionary.Add("EUR/JPY",138.376);
            dicInitDictionary.Add("GBP/JPY",194.409);
            dicInitDictionary.Add("GBP/CHF",1.47071);
            dicInitDictionary.Add("EUR/GBP",0.71353);
            dicInitDictionary.Add("AUD/JPY",95.627 );
            
            List<Candle> lstIntList = new List<Candle>();
            Share sCurrShare = Program.SharesList[strSymbol];

            for (int nIndex = 0; nIndex < Consts.NUM_OF_CANDLES; nIndex++)
            {
                Candle cCurrCandle = new Candle(dtStartTime.ToMarketTime(),
                                                nIndex > 0 ? lstIntList[nIndex - 1] : null,
                                                dicInitDictionary[strSymbol],                                                
                                                dicInitDictionary[strSymbol],
                                                dicInitDictionary[strSymbol],
                                                dicInitDictionary[strSymbol],
                                                0,
                                                0,
                                                0,
                                                0);

                lstIntList.Add(cCurrCandle);
            }


            cCandleList = new CandlesList(lstIntList, sCurrShare, bIsPrimary);
            cCandleList.LastCandle = lstIntList[cCandleList.Count - 1];

            return (cCandleList);
        }
    }

    public static class ExtensionMethods
    {
        public static byte[] ToBytesArray(this string str)
        {
            byte[] arrBytesArray = new byte[str.Length];

            for (int nCurrCharIndex = 0; nCurrCharIndex < str.Length; nCurrCharIndex++)
            {
                arrBytesArray[nCurrCharIndex] = (byte)(str[nCurrCharIndex]);
            }

            return (arrBytesArray);
        }
        public static MarketTime ToMarketTime(this DateTime dtDate)
        {
            return (new MarketTime(dtDate.Hour, dtDate.Minute));
        }
    }
} 
