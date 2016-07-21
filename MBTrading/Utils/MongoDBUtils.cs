using System.Collections.Concurrent;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;

namespace MBTrading.Utils
{
    public static class MongoDBUtils
    {
        private static bool ActivateMongo = true;
        private static MongoClient Client;
        private static MongoServer Server;
        private static MongoDatabase Database;
        private static MongoCollection DashboardCollection;
        private static MongoCollection CandlesticksCollection;

        static MongoDBUtils()
        {
            if (ActivateMongo)
            {
                MongoDBUtils.Connect();
                MongoDBUtils.DashboardCollection.RemoveAll();
                MongoDBUtils.CandlesticksCollection.RemoveAll();
                MongoDBUtils.InitializeCandlesticksCollection();
                MongoDBUtils.InitializeDashboardCollection();
                BsonClassMap.RegisterClassMap<Share>(cm =>
                {
                    cm.MapProperty(S => S.Symbol);
                    cm.MapProperty(S => S.IsPosition);
                    cm.MapProperty(S => S.OffLineIsPosition);
                    cm.MapProperty(S => S.CurrPL);
                    cm.MapProperty(S => S.TotalPL);
                });
            }
        }
        public static void Connect()
        {
            if (ActivateMongo)
            {
                MongoDBUtils.Client = new MongoClient();
                MongoDBUtils.Server = Client.GetServer();
                MongoDBUtils.Database = Server.GetDatabase("webTradingDB");
                MongoDBUtils.DashboardCollection = Database.GetCollection("Dashboard");
                MongoDBUtils.CandlesticksCollection = Database.GetCollection("Candlesticks");
            }
        }

        public static void InitializeCandlesticksCollection()
        {
            if (ActivateMongo)
            {
                try
                {
                    foreach (string currShareSymbol in Program.SymbolsNamesList.Keys)
                    {
                        BsonDocument doc = new BsonDocument 
                {
                     { "Symbol", currShareSymbol },
                     { "Candles", new BsonArray {} }
                };

                        MongoDBUtils.CandlesticksCollection.Insert(doc);
                    }
                }
                catch
                {
                    MongoDBUtils.Connect();
                }
            }
        }
        public static void InitializeDashboardCollection()
        {
            if (ActivateMongo)
            {
                try
                {
                    foreach (string currShareSymbol in Program.SymbolsNamesList.Keys)
                    {
                        BsonDocument doc = new BsonDocument 
                    {
                        { "DateTime", new BsonDateTime(DateTime.Now) },
                        { "AccountBallance", Consts.QUANTITY },
                        { "Symbol", currShareSymbol },
                        { "TransactionPL", 0 }
                    };

                        MongoDBUtils.DashboardCollection.Insert(doc);
                    }
                }
                catch
                {
                    MongoDBUtils.Connect();
                }
            }
        }
        
        public static void DBEventAfterPositionSell(double dAccountBallance, string strSymbol, double dTransactionPL, int nCandleIndex, int nDuration, double dBuyPrice, double dSellPrice)
        {
            if (ActivateMongo)
            {
                try
                {
                    BsonDocument doc = new BsonDocument 
                {
                     { "DateTime", new BsonDateTime(DateTime.Now) },
                     { "AccountBallance", dAccountBallance },
                     { "Symbol", strSymbol },
                     { "TransactionPL", dTransactionPL },
                     { "CandleIndex" , nCandleIndex },
                     { "Duration" , nDuration },
                     { "BuyPrice" , dBuyPrice },
                     { "SellPrice" , dSellPrice }
                };

                    MongoDBUtils.DashboardCollection.Insert(doc);
                    PushServer.SendTCPMessage("2");
                }
                catch
                {
                    MongoDBUtils.Connect();
                }
            }
        }
        public static void DBEventAfterCandleFinished(Share sShare, Candle cFinishedCandle)
        {
            if (ActivateMongo)
            {
                try
                {
                    BsonDocument doc = new BsonDocument 
                     {
                         { "CandleIndex", sShare.OffLineCandleIndex + sShare.CandleIndex },
                         { "Date", new BsonDateTime(DateTime.Now) },
                         { "Open", cFinishedCandle.R_Open.ToString() },
                         { "Close", cFinishedCandle.R_Close.ToString() },
                         { "High", cFinishedCandle.R_High.ToString() },
                         { "Low", cFinishedCandle.R_Low.ToString() },
                         { "WMA", cFinishedCandle.EndWMA },
                         { "EMA", cFinishedCandle.EndEMA },
                         { "BHIGH", sShare.CandlesList.SMA.UpperBollinger },
                         { "BLOW", sShare.CandlesList.SMA.LowerBollinger },
                         { "IsPossition" , ((sShare.OffLineIsPosition) || (sShare.IsPosition)) },
                         { "BuyIndicator", ((sShare.BuyIndex == sShare.CandleIndex) || (sShare.OffLineBuyIndex == sShare.OffLineCandleIndex)) },
                         { "SellIndicator", ((sShare.SellIndex == sShare.CandleIndex) || (sShare.OffLineSellIndex == sShare.OffLineCandleIndex)) },
                         { "BuyPrice", sShare.AverageBuyPrice },
                         { "SellPrice", sShare.SellPrice}
                    };

                    MongoDBUtils.CandlesticksCollection.Update(Query.EQ("Symbol", sShare.Symbol), Update.Push("Candles", doc));
                }
                catch
                {
                    MongoDBUtils.Connect();
                }
            }
        }
    }
}
