using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MBTrading.Utils;

namespace MBTrading.Entities
{
    public class NNOrder
    {
        public Share ParentShare;
        public bool Strategy;
        public double Stop;
        public double Buy;
        public double PL;
        public int ProfitIndicator;
        public bool CrossIndicator;
        public int LiveCounter = 0;
        public List<Candle> CandlesHistory;

        public NNOrder(Share sParentShare, double dStop, double dBuy, List<Candle> lstCandles, bool bStrategy)
        {
            this.ParentShare = sParentShare;
            this.CrossIndicator = false;
            this.Strategy = bStrategy;
            this.Stop = dStop;
            this.Buy = dBuy;
            this.CandlesHistory = new List<Candle>();
            this.CandlesHistory.AddRange(lstCandles);     
        }
        public void NNSell(double dCurrentPrice)
        {
            this.PL = FixGatewayUtils.CalculateProfit(this.Buy, dCurrentPrice, this.ParentShare.Symbol, Consts.QUANTITY);

            if (this.PL > 0)
            {
                this.ProfitIndicator = 1;
                this.ParentShare.CandlesList.P++;
            }
            else
            {
                if (this.LiveCounter > 10)
                { this.ProfitIndicator = 1; this.ParentShare.CandlesList.P++; }
                else
                { this.ProfitIndicator = 0; this.ParentShare.CandlesList.N++; }
            }

            if (this.ParentShare.CandlesList.NeuralNetworkSelfAwarenessCollection.Count == Consts.NEURAL_NETWORK_NUM_OF_TRAINING_CANDLES)
            { this.ParentShare.CandlesList.NeuralNetworkSelfAwarenessCollection.RemoveAt(0); }
            this.ParentShare.CandlesList.NeuralNetworkSelfAwarenessCollection.Add(this);
        }
        public void CheckUpdateStop()
        {
            Candle cBeforePreviousCandle = this.ParentShare.CandlesList.Candles[this.ParentShare.CandlesList.CountDec - 2];
            Candle cPreviousCandle = this.ParentShare.CandlesList.Candles[this.ParentShare.CandlesList.CountDec - 1];

            // This section waites from the moment that MA's cross each other, to the moment that WMA direction is changing UP again but still bellow the EMA
            // Potential Sell conditions - MA's crossed - WMA bellow EMA - Starting of a Downward
            bool bCrossMA = ((this.ParentShare.CandlesList.PrevCandle.StartWMA - this.ParentShare.CandlesList.PrevCandle.StartEMA > this.ParentShare.PipsUnit * this.ParentShare.D_MilitraizedZone) &&
                             (this.ParentShare.CandlesList.WMA - this.ParentShare.CandlesList.EMA <= this.ParentShare.PipsUnit * this.ParentShare.D_MilitraizedZone));

            // Set the indicator to True wen => Cross occurd || already set it befor to True
            this.CrossIndicator = (bCrossMA) || (this.CrossIndicator);

            if (!cBeforePreviousCandle.WMADirection && cPreviousCandle.WMADirection && this.CrossIndicator && this.ParentShare.CandlesList.EMA > this.ParentShare.CandlesList.WMA)
            {
                this.Stop = Math.Min(cBeforePreviousCandle.Low, cPreviousCandle.Low) - (this.ParentShare.PipsUnit * 2);
                this.CrossIndicator = false;
            }
        }
    }

    public class NNOrdersList
    {
        public List<NNOrder> OrdersList;
        private List<NNOrder> OrdersToSell;
        public NNOrdersList()
        {
            this.OrdersList = new List<NNOrder>();
            this.OrdersToSell = new List<NNOrder>();
        }
        public void Add(NNOrder nnoToAdd)
        {
            if ((nnoToAdd.Stop < nnoToAdd.Buy)) // &&
                //(!nnoToAdd.ParentShare.CandlesList.PrevWMADirection) &&
                //(nnoToAdd.ParentShare.CandlesList.WMADirection) &&
                //(nnoToAdd.ParentShare.CandlesList.WMA < nnoToAdd.ParentShare.CandlesList.EMA)) 
            {
                this.OrdersList.Add(nnoToAdd);
            }
        }
        public void CheckOrders(double dCurrentPrice, bool bNewCandle)
        {
            // Run over all orders
            foreach (NNOrder nnoCurrOrder in this.OrdersList)
            {
                if (bNewCandle)
                { nnoCurrOrder.LiveCounter++; }

                nnoCurrOrder.CheckUpdateStop();
                
                // If stop price is less than current price - "sell" it
                if (nnoCurrOrder.Stop >= dCurrentPrice)
                {
                    this.OrdersToSell.Add(nnoCurrOrder);
                }
            }

            // Run over all sold orders
            foreach (NNOrder nnoCurrOrder in this.OrdersToSell)
            {
                nnoCurrOrder.NNSell(dCurrentPrice);
                this.OrdersList.Remove(nnoCurrOrder);
            }

            this.OrdersToSell.Clear();
        }
    }
}
