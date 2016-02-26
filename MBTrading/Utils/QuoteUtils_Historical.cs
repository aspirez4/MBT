using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Xml;

namespace MBTrading
{
    public static class QuoteUtils_Historical
    {
        // Static Data Members
        private static string SessionToken = string.Empty;
        private static string HistoricalServer = string.Empty;
        private static string QuoteServer = string.Empty;
         
        // Ctor
        static QuoteUtils_Historical()
        {
            ConnectHistoricalServer();
            new Thread(ReConnectHistoricalServer).Start();
        }
        public static void ConnectHistoricalServer()
        {
            // Get MBQuateAPI connecting details
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.mbtrading.com/secure/getquoteserverxml.aspx?username=MyDemoUser91&password=547d38f5546ae2a658eecfa541d4a9b7ee1898208ec8706d7b26b13e8e372173");
            using (WebResponse response = request.GetResponse())
            {
                // Read response xml
                using (StreamReader responseReader = new StreamReader(response.GetResponseStream()))
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(responseReader);
                    XmlNode xmlLogins = (xmlDoc.GetElementsByTagName("logins") as XmlNodeList)[0];

                    // Initialize the connections members
                    QuoteUtils_Historical.SessionToken = xmlLogins.Attributes["SessionToken"].Value;
                    QuoteUtils_Historical.QuoteServer = xmlLogins.Attributes["quote_Server"].Value;
                    QuoteUtils_Historical.HistoricalServer = xmlLogins.Attributes["hist_server"].Value;
                }
            }
        }
        public static void ReConnectHistoricalServer()
        {
            // Wait 20 hours and than re-connect
            while (Program.IsProgramAlive)
            {
                Thread.Sleep(72000000);
                ConnectHistoricalServer();
            }
        }
        public static CandlesList LoadLast20(string strSymbol, int nMinuteBar, bool bIsPrimary, DateTime dtStartTime, DateTime dtEndTime)
        {
            CandlesList cCandleList;
            DateTime dtPreStartTime = dtEndTime.Subtract(new TimeSpan(4, 0, 0, 0, 0));

            // Initialize the Get Request for historical muiutes bars
            string strHistRequest = string.Format("http://{0}/GetMinBars.ashx?SessionToken={1}&Symbol={2}&StartTime={3}&EndTime={4}&Period={5}",
                                                  QuoteUtils_Historical.HistoricalServer,
                                                  QuoteUtils_Historical.SessionToken,
                                                  strSymbol,
                                                  dtPreStartTime.ToString("yyyy-MM-ddTHH:mm"),
                                                  dtEndTime.ToString("yyyy-MM-ddTHH:mm"),
                                                  nMinuteBar * 60);

            // Send the request Asynchronously
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(strHistRequest);

            using (WebResponse response = request.GetResponse())
            {
                // Read response xml
                using (StreamReader responseReader = new StreamReader(response.GetResponseStream()))
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(responseReader);
                    XmlNodeList lstMinBarList = (xmlDoc.GetElementsByTagName("MinBar") as XmlNodeList);
                    Share sCurrShare = Program.SharesList[strSymbol];
                    List<Candle> lstIntList = new List<Candle>();
                    int nStopIndex = Math.Max(0,lstMinBarList.Count - Consts.NUM_OF_CANDLES);
                    int nPrev = -1;
                    for (int nIndex = lstMinBarList.Count - 1; nIndex >= nStopIndex; nIndex--)
                    {
                        Candle cCurrCandle = new Candle(Convert.ToDateTime(lstMinBarList[nIndex].Attributes["date"].Value).ToMarketTime(),
                                                        nPrev > -1 ? lstIntList[nPrev] : null,
                                                        double.Parse(lstMinBarList[nIndex].Attributes["open"].Value),
                                                        double.Parse(lstMinBarList[nIndex].Attributes["close"].Value),
                                                        double.Parse(lstMinBarList[nIndex].Attributes["high"].Value),
                                                        double.Parse(lstMinBarList[nIndex].Attributes["low"].Value),
                                                        0,
                                                        0,
                                                        0,
                                                        0);

                        lstIntList.Add(cCurrCandle);
                        nPrev++;
                    }


                    cCandleList = new CandlesList(lstIntList, sCurrShare, bIsPrimary);
                    cCandleList.LastCandle = lstIntList[cCandleList.Count - 1];

                    for (int nIndex = lstMinBarList.Count - Consts.NUM_OF_CANDLES - 1; nIndex >= 0; nIndex--)
                    {
                        MarketTime mt = Convert.ToDateTime(lstMinBarList[nIndex].Attributes["date"].Value).ToMarketTime();
                        MarketData m1 = new MarketData(MarketDataType.Volume, 0, double.Parse(lstMinBarList[nIndex].Attributes["open"].Value), mt);
                        MarketData m2 = new MarketData(MarketDataType.Volume, 0, double.Parse(lstMinBarList[nIndex].Attributes["low"].Value), mt);
                        MarketData m3 = new MarketData(MarketDataType.Volume, 0, double.Parse(lstMinBarList[nIndex].Attributes["high"].Value), mt);
                        MarketData m4 = new MarketData(MarketDataType.Volume, 0, double.Parse(lstMinBarList[nIndex].Attributes["close"].Value), mt);
                        cCandleList.AddOrUpdatePrice(m1);
                        cCandleList.AddOrUpdatePrice(m2);
                        cCandleList.AddOrUpdatePrice(m3);
                        cCandleList.AddOrUpdatePrice(m4);
                    }
                }
            }

            return (cCandleList);
        }

        public static void TimeoutCallback(object state, bool timedOut)
        {
            if (timedOut)
            {
                HttpWebRequest request = state as HttpWebRequest;
                if (request != null)
                {
                    request.Abort();
                }
            }
        }
    }
}