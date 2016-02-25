
using System.IO;
using System.Collections.Generic;
using System;

namespace MBTrading
{
    public class Candle
    {
        // Data Members
        public int      CandleIndex;
        public MarketTime StartDate;
        public double   R_Open;
        public double   R_Close;
        public double   R_High;
        public double   R_Low;
        public double   Open;
        public double   Close;
        public double   High;
        public double   Low;
        public double   Bid;
        public double   Ask;
        private double  StartVolume;
        public double   LastVolume;
        public double   EndWMA;
        public double   EndEMA;
        public double   EndTDI_Green;
        public double   EndTDI_Red;
        public double   EndTDI_Mid;
        public double   EndTDI_Upper;
        public double   EndTDI_Lower;
        public bool     WMADirection;
        public bool     EMADirection;
        public double   StartWMA;
        public double   StartEMA;
        public double   StartTDI_Green;
        public double   StartTDI_Red;
        public int      NumOfPeiceUpdates;
        public double   PricesSum;
        public double   ZigZagPrediction;
        public List<double> ExtraList;
        public double   CandleVolume { get { if (this.StartVolume > this.LastVolume) { this.StartVolume = 0; } return (this.LastVolume - this.StartVolume);}  }

        // Ctor
        public Candle(MarketTime mtStartDate,
                      Candle cPrev,
                      double dOpen, 
                      double dClose, 
                      double dHigh, 
                      double dLow, 
                      double dBid,
                      double dAsk,
                      double dVolume,
                      int    nCandleNo)
        {
            this.ExtraList    = new List<double>();
            this.StartDate    = mtStartDate;
            this.R_Open       = dOpen;
            this.R_Close      = dClose;
            this.R_High       = dHigh;
            this.R_Low        = dLow;
            this.Close        = (this.R_Close + this.R_High + this.R_Low + this.R_Open) / 4;
            this.Open         = (((cPrev == null) ? (this.R_Open) : (cPrev.Open)) + R_Close) / 2;
            this.High         = Math.Max(this.R_High, Math.Max(this.Open, this.Close));
            this.Low          = Math.Min(this.R_Low,  Math.Min(this.Open, this.Close));
            this.Bid          = dBid;
            this.Ask          = dAsk;
            this.StartVolume  = dVolume;
            this.LastVolume   = dVolume;
            this.PricesSum    = dOpen;
            this.CandleIndex     = nCandleNo;
            this.NumOfPeiceUpdates = 1;
            this.ZigZagPrediction = 0;
        }

        public void UpdateCandle(MarketData mdCurrMarketData)
        {
            switch (mdCurrMarketData.DataType)
            {
                case (MarketDataType.Bid):
                {
                    this.Bid = mdCurrMarketData.Value;
                    break;
                }
                case (MarketDataType.Ask):
                {
                    this.Ask = mdCurrMarketData.Value;
                    break;
                }
                case (MarketDataType.Volume):
                {
                    this.LastVolume = mdCurrMarketData.Value;

                    if (mdCurrMarketData.Price != -1)
                    {
                        this.NumOfPeiceUpdates++;
                        this.PricesSum += mdCurrMarketData.Price;

                        this.R_Close = mdCurrMarketData.Price;
                        if (this.R_High < mdCurrMarketData.Price) { this.R_High = mdCurrMarketData.Price; }
                        if (this.R_Low > mdCurrMarketData.Price) { this.R_Low = mdCurrMarketData.Price; }

                        this.Close = (this.R_Close + this.R_High + this.R_Low + this.R_Open) / 4;
                        this.High = Math.Max(this.R_High, Math.Max(this.Open, this.Close));
                        this.Low = Math.Min(this.R_Low, Math.Min(this.Open, this.Close));
                    }
                    
                    break;
                }
                default:
                {
                    break;
                }
            }
        }
        public void LogCandle(Share sParentShare, double dExtra)
        {
            try
            {
                File.AppendAllText(string.Format("{0}\\Candles\\{1}.txt", Consts.FilesPath, sParentShare.Symbol.Remove(3, 1)),
                   //string.Format("{0};o:{1};c:{2};h:{3};l:{4};w:{5};e:{6};a:{7};b:{8};co:{9};hl:{10};hc:{11};cl:{12};ab:{13};v:{14};i:{15}\n",
                   string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{10};{11};{12};{13};{14};{15};{17};{18}\n",
                                 sParentShare.Symbol,
                                 this.R_Open,
                                 this.R_Close,
                                 this.R_High,
                                 this.R_Low,
                                 this.EndWMA,
                                 this.EndEMA,
                                 dExtra,
                                 this.Ask,
                                 this.Bid,
                                 this.ExtraList[this.ExtraList.Count - 4],
                                 this.ExtraList[this.ExtraList.Count - 3],
                                 this.ExtraList[this.ExtraList.Count - 2],
                                 this.ExtraList[this.ExtraList.Count - 1],
                                 this.ExtraList[1],
                                 this.ExtraList[2],
                                 this.PricesSum / this.NumOfPeiceUpdates,
                                 this.CandleVolume,
                                 sParentShare.OffLineCandleIndex));
            }
            catch { }
        }
    }
}