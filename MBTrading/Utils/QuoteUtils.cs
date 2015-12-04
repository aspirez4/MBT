using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;
using System.Collections;


namespace MBTrading
{
    public class SetML
    {
        public int PatternLength;
        public int OutcomeInterval;
        public double Outcome;
        public SetML(int nPatternLength,
                     int nOutcomeInterval,
                     double dOutcome)
        {
            this.PatternLength = nPatternLength;
            this.OutcomeInterval = nOutcomeInterval;
            this.Outcome = dOutcome;
        }
    }

    public static class QuoteUtils
    {
        // Static Data Members
        private static TcpClient        tcpClient                       = null;
        private static NetworkStream    networkStream                   = null;
        private static string           SessionToken                    = string.Empty;
        private static string           HistoricalServer                = string.Empty;
        private static string           QuoteServer                     = string.Empty;
        public  static bool             LogonIndicator = false;
        private static int              MarketDataStreamArraySize;
        private static int              MarketDataStreamArrayUsageSize;
        public  static DateTime         LastHB;
        private static StringBuilder    ParserStringBuilder;
        private static Thread           ListenerThread;
        private static Thread           ConnectivityCheckThread;
        private static long             _lockFlag   = 0;
        private static long             _T_Flag     = 1;
        private static long             _F_Flag     = 0;

        // Ctor
        static QuoteUtils()
        {
            QuoteUtils.ParserStringBuilder = new StringBuilder();
            QuoteUtils.MarketDataStreamArraySize = 50000;
            QuoteUtils.MarketDataStreamArrayUsageSize = QuoteUtils.MarketDataStreamArraySize - 10;
        }

        // Static Methods - Quote API
        public static   void ConnectMBTQuoteAPI()
        {
            if (Consts.WorkOffLineMode)
            {
                string strQuotesFolder = "C:\\Users\\Or\\Projects\\Quotes\\Processed\\";
                IEnumerator i = Program.SharesList.Values.GetEnumerator();
                i.MoveNext();
                Share sShare = (Share)i.Current;
                
                for (int nIndex = 0; nIndex < Directory.GetFiles(strQuotesFolder).Length; nIndex++)
                {
                    byte[] strLines = File.ReadAllBytes(string.Format("{0}Quotes{1}.txt", strQuotesFolder, nIndex));
                    strLines = QuoteUtils.ConCut(strLines, strLines.Length);
                    QuoteUtils.ParseQuotes(strLines);

                    while (sShare.PricesQueue.Count != 0)
                    {
                        Thread.Sleep(200);
                    }
                }

                foreach (string strCurrFile in Directory.GetFiles(Consts.FilesPath + "\\Candles\\"))
                {
                    File.AppendAllText(Consts.FilesPath + "\\Candles\\a.txt", File.ReadAllText(strCurrFile));
                }
            }
            else
            {
                if (!QuoteUtils.LogonIndicator)
                {
                    try
                    {
                        // Get MBQuateAPI connecting details
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format("https://www.mbtrading.com/secure/getquoteserverxml.aspx?username={0}&password={1}", Consts.Web_UserName, Consts.Web_Password));

                        using (WebResponse response = request.GetResponse())
                        {
                            // Read response xml
                            using (StreamReader responseReader = new StreamReader(response.GetResponseStream()))
                            {
                                XmlDocument xmlDoc = new XmlDocument();
                                xmlDoc.Load(responseReader);
                                XmlNode xmlLogins = (xmlDoc.GetElementsByTagName("logins") as XmlNodeList)[0];

                                // Initialize the connections members
                                QuoteUtils.SessionToken = xmlLogins.Attributes["SessionToken"].Value;
                                QuoteUtils.QuoteServer = xmlLogins.Attributes["quote_Server"].Value;
                                QuoteUtils.HistoricalServer = xmlLogins.Attributes["hist_server"].Value;
                            }
                        }

                        QuoteUtils.Logon();
                    }
                    catch (Exception e)
                    {
                        Loger.ErrorLog(e);
                    }
                }
            }
        }
        private static  void Logon()
        {
            QuoteUtils.LogonIndicator = false;

            // If there is no other thread preforming this method
            if (Interlocked.CompareExchange(ref _lockFlag, _T_Flag, _F_Flag) == _F_Flag)
            {
                if ((!QuoteUtils.LogonIndicator) && (QuoteUtils.ListenerThread != null))
                    QuoteUtils.ListenerThread.Abort();

                try
                {
                    byte[] loginAcceptedStream = null;

                    while (!QuoteUtils.LogonIndicator)
                    {
                        tcpClient = new TcpClient();
                        QuoteUtils.tcpClient.Connect(QuoteUtils.QuoteServer, 5020);
                        QuoteUtils.networkStream = QuoteUtils.tcpClient.GetStream();
                        QuoteUtils.networkStream.ReadTimeout = 120000;
                        // Login to server
                        byte[] loginStream = System.Text.Encoding.ASCII.GetBytes("L|100=MyDemoUser91;133=547d38f5546ae2a658eecfa541d4a9b7ee1898208ec8706d7b26b13e8e372173\n");
                        QuoteUtils.networkStream.Write(loginStream, 0, loginStream.Length);
                        QuoteUtils.networkStream.Flush();

                        // Get accept response
                        loginAcceptedStream = new byte[(int)tcpClient.ReceiveBufferSize + 100];
                        QuoteUtils.networkStream.Read(loginAcceptedStream, 0, (int)tcpClient.ReceiveBufferSize);
                        QuoteUtils.networkStream.Flush();

                        // Login Accepted
                        bool bSuccess = ((loginAcceptedStream[0] == (byte)71) && (loginAcceptedStream[1] == (byte)124));

                        if (bSuccess)
                        {
                            QuoteUtils.ListenerThread = new Thread(QuoteUtils.ListenCurrentMarketData);
                            QuoteUtils.ListenerThread.Start();
                            if (QuoteUtils.ConnectivityCheckThread == null)
                            {
                                QuoteUtils.ConnectivityCheckThread = new Thread(QuoteUtils.ConnectivityCheck);
                                QuoteUtils.ConnectivityCheckThread.Start();
                            }
                            new Thread(QuoteUtils.SubscribeAllShares).Start();
                        }

                        
                        QuoteUtils.LogonIndicator = bSuccess;

                        Thread.Sleep(90000);
                    }
                }
                catch (Exception e)
                {
                    Loger.ErrorLog(e);
                }
                finally
                {
                    // Free the lock
                    Interlocked.Decrement(ref _lockFlag);
                }
            }
        }
        public static   void ConnectivityCheck()
        {
            TimeSpan dtHBDelta = new TimeSpan(0, 1, 0);
            while (Program.IsProgramAlive)
            {
                if (DateTime.Now.Subtract(QuoteUtils.LastHB) > dtHBDelta)
                {
                    QuoteUtils.SendHeartBeat();
                    Thread.Sleep(10000);

                    if ((DateTime.Now.Subtract(QuoteUtils.LastHB) > dtHBDelta) && (!Time.IsWeekendNow()))
                    {
                        new Thread(QuoteUtils.Logon).Start();
                    }
                }
                Thread.Sleep(30000);
            }
        }





        public static void CheckIfConnectionError(Exception e)
        {
            if ((e.Message.Contains("An established connection was aborted by the software in your host machine")) ||
                (e.Message.Contains("No connection could be made because the target machine actively refused it")) ||
                (e.Message.Contains("An existing connection was forcibly closed by the remote host")) ||
                (e.Message.Contains("Cannot access a disposed object")) ||
                (e.Message.Contains("Object reference not set to an instance of an object")) ||
                (e.Message.Contains("A connection attempt failed because the connected party did not properly respond after a period of time")))
            {
                QuoteUtils.LogonIndicator = false;
                if (QuoteUtils.tcpClient != null)
                    QuoteUtils.tcpClient.Close();
                QuoteUtils.tcpClient = null;
                QuoteUtils.networkStream = null;
                new Thread(QuoteUtils.Logon).Start();
            }
        }


        public static void SubscribeAllShares()
        {
            // Subscribe all shares
            while (Time.IsWeekendNow()) { Thread.Sleep(60000); }
            foreach (string strCurrSymbol in Program.SymbolsNamesList.Keys)
            {
                QuoteUtils.Level1Subscription(strCurrSymbol);
            }
        }
        public static void Level1Subscription(string strSymbol)
        {
            // Subscribe
            byte[] subscriptionStream = System.Text.Encoding.ASCII.GetBytes(string.Format("S|1003={0};2000=20000\n", strSymbol));
            networkStream.Write(subscriptionStream, 0, subscriptionStream.Length);
            networkStream.Flush();
        }
        public static void SendHeartBeat()
        {
            try
            {
                // HeartBeat
                byte[] HeartBeatStream = System.Text.Encoding.ASCII.GetBytes("9|\n");
                networkStream.Write(HeartBeatStream, 0, HeartBeatStream.Length);
                networkStream.Flush();
            }
            catch (Exception e)
            {
                Loger.ErrorLog(e);
                QuoteUtils.CheckIfConnectionError(e);
            }
        }

        public static byte[] ConCut(byte[] arr, int nEOA)
        {
            int nArrayLength = arr.Length + 10000;
            byte[] arrToReturn = new byte[nArrayLength];
            for (int nIndex = 0; nIndex < nEOA; nIndex++) { arrToReturn[nIndex] = arr[nIndex]; }
            return (arrToReturn);
        }
        public static void ListenCurrentMarketData()
        {
            byte[] marketDataStream;
            int nNumberOfBytesRead;
            int nLastIndex;

            // While program open
            while (Program.IsProgramAlive)
            {
                try
                {
                    marketDataStream = new byte[QuoteUtils.MarketDataStreamArraySize];
                    nNumberOfBytesRead = 0;
                    nLastIndex = 0;

                    do
                    {
                        // Try to read              
                        nNumberOfBytesRead = networkStream.Read(marketDataStream, nLastIndex, marketDataStream.Length - nLastIndex);

                        if (networkStream.DataAvailable)
                        {
                            nLastIndex += nNumberOfBytesRead;
                            if (nLastIndex == marketDataStream.Length) { marketDataStream = QuoteUtils.ConCut(marketDataStream, nLastIndex); }
                        }
                    }
                    while (networkStream.DataAvailable);

                    QuoteUtils.ParseQuotes(marketDataStream);
                }
                catch (Exception e)
                {
                    Loger.ErrorLog(e);
                    QuoteUtils.CheckIfConnectionError(e);
                }

                Thread.Sleep(100);
            }
        }
        public static void ParseQuotes(byte[] arrMarketDataStream)
        {
            // 1003 - 858796081 - Symbol
            // 2002 - 842018866 - Last price
            // 2003 - 858796082 - Bid - Price i can sell
            // 2004 - 875573298 - Ask - Price i can buy
            // 2005 - 892350514 - Bid size
            // 2006 - 909127730 - Ask size
            // 2007 - 925904946 - Last size
            // 2012 - 842084402 - Total volume today
            // 2014 - 875638834 - Time

            ParserStringBuilder.Clear();
            string strSymbol;
            string strTime;
            double dValue = -1;
            double dPrice = -1;
            int nIndex = 0;
            int nRowIndex = 0;
            int nKey = 0;
            int nValKey = 0;
            int nHour;
            int nMinute;
            int nAllData = 0;
            bool nLastDataIndicatior = false;
            MarketData mdCurrData;


            // Update the Last-HB
            if (arrMarketDataStream[nIndex] != 0)
                QuoteUtils.LastHB = DateTime.Now;

            while (arrMarketDataStream[nIndex] != 0)
            {
                // Start Of Quote (Line)  "1|......"
                if ((arrMarketDataStream[nIndex] == 49) && (arrMarketDataStream[nIndex + 1] == 124))
                {
                    strSymbol = string.Empty;
                    strTime = string.Empty;
                    dValue = -1;
                    dPrice = -1;
                    nHour = -1;
                    nMinute = -1;
                    nAllData = 0;
                    nLastDataIndicatior = false;

                    // Run to the EOL
                    for (nRowIndex = nIndex; ((arrMarketDataStream[nRowIndex] != 10) && (arrMarketDataStream[nRowIndex] != 0)); nRowIndex++)
                    {
                        // If arr[n] == ';' OR arr[n] == '|' 
                        if ((arrMarketDataStream[nRowIndex] == 59) || (arrMarketDataStream[nRowIndex] == 124))
                        {
                            nKey = BitConverter.ToInt32(arrMarketDataStream, nRowIndex + 1);
                            nRowIndex += 6;

                            // If Key == 1003
                            if (nKey == 858796081)
                            {
                                ParserStringBuilder.Clear();

                                // Get all values
                                for (; ((arrMarketDataStream[nRowIndex] != 59) &&
                                        (arrMarketDataStream[nRowIndex] != 10) &&
                                        (arrMarketDataStream[nRowIndex] != 0)); nRowIndex++)
                                {
                                    ParserStringBuilder.Append((char)arrMarketDataStream[nRowIndex]);
                                }

                                nRowIndex--;
                                strSymbol = ParserStringBuilder.ToString();
                                nAllData++;
                            }
                            // If Key == 2002 - 842018866 - Last price
                            //           2003 - 858796082 - Bid - Price i can sell
                            //           2004 - 875573298 - Ask - Price i can buy
                            //           2012 - 842084402 - Total volume today
                            else if ((nKey == 842018866) ||
                                     (nKey == 858796082) ||
                                     (nKey == 875573298) ||
                                     (nKey == 842084402))
                            {
                                ParserStringBuilder.Clear();

                                // Get all values
                                for (; ((arrMarketDataStream[nRowIndex] != 59) &&
                                        (arrMarketDataStream[nRowIndex] != 10) &&
                                        (arrMarketDataStream[nRowIndex] != 0)); nRowIndex++)
                                {
                                    ParserStringBuilder.Append((char)arrMarketDataStream[nRowIndex]);
                                }

                                nRowIndex--;
                                nValKey = nKey;

                                // If the price taken from the last line and ther's a chance it has been cut - clear it
                                if (arrMarketDataStream[nRowIndex] == 0) { ParserStringBuilder.Clear(); }
                                if (double.TryParse(ParserStringBuilder.ToString(), out dValue))
                                {
                                    if (nKey == 842018866) { dPrice = dValue; }
                                    nAllData++;
                                }
                            }
                            // If Key == 2014
                            else if (nKey == 875638834)
                            {
                                nHour = ((arrMarketDataStream[nRowIndex++] - 48) * 10) + (arrMarketDataStream[nRowIndex++] - 48);
                                nRowIndex++;
                                nMinute = ((arrMarketDataStream[nRowIndex++] - 48) * 10) + (arrMarketDataStream[nRowIndex++] - 48);
                                nRowIndex++;
                                nRowIndex++;

                                nAllData++;
                                nLastDataIndicatior = true;
                            }

                            // If end of relevant data - go till the end of the line
                            if (nLastDataIndicatior)
                            {
                                while ((arrMarketDataStream[nRowIndex] != 10) && (arrMarketDataStream[nRowIndex] != 0)) nRowIndex++;
                                break;
                            }
                        }
                    }


                    if ((nLastDataIndicatior) && (nAllData > 2))
                    {
                        mdCurrData = new MarketData(nValKey, dValue, dPrice, new MarketTime(nHour, nMinute));
                            
                        // Enqueue the last price to the Prices Queue 
                        try { Program.SharesList[strSymbol].PricesQueue.Enqueue(mdCurrData); }
                        catch { }
                    }

                    nIndex = nRowIndex;
                }

                nIndex++;
            }

            // File.AppendAllText(string.Format("{0}\\Quotes\\Quotes{1}.txt", Consts.FilesPath, Program.nDayNum), System.Text.Encoding.Default.GetString(arrMarketDataStream, 0, nIndex));
        }
    }
}
