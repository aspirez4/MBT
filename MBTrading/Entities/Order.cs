
using System.Threading;
using MBTrading.Entities;
using System;
namespace MBTrading
{
    public enum OrderFGWResponse
    {
        DidntGet,
        PositiveConfirmed,
        Rejected
    };

    public class Order
    {
        public string ClientOrdID;
        public string Sequence;
        public string DateTimeUTC;
        public string Gateway_OrderID;
        public double Gateway_AVGEXEPrice;
        public double? Gateway_Limit;
        public double? Gateway_Stop;
        public double? Gateway_StopLoss;
        public double? Limit;
        public double? Stop;
        public double? StopLoss;
        private string ConnectedStopLossClientOrdID;
        public int OrderWantedQuantity;
        public int OrderFilledQuantity;
        public string Symbol;
        public Share ParrentShare;
        public int IndexTTL;
        public bool WasSent;
        public OrderFGWResponse GatewayStatusResponse;
        public bool IsBuy;
        public bool IsLong;


        public Order(string strSymbol, 
                     int nWantedQuantit,
                     double? Limit,
                     double? Stop,
                     double? StopLoss, 
                     bool bIsBuy,
                     bool bIsLong, 
                     int? nCandleIndexTTL)
        {
            this.Symbol = strSymbol;
            this.ParrentShare = Program.SharesList[strSymbol];

            this.Sequence = FixGatewayUtils.GetSecureSequence().ToString();
            this.DateTimeUTC = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss");
            this.ClientOrdID = string.Format("{0}{1}", Sequence, DateTimeUTC);
            this.Limit = Limit;
            this.Stop = Stop;
            this.StopLoss = StopLoss;
            this.IsBuy = bIsBuy;
            this.IsLong = bIsLong;

            this.WasSent = false;
            this.GatewayStatusResponse = OrderFGWResponse.DidntGet;
            this.ConnectedStopLossClientOrdID = string.Empty;
            this.IndexTTL = nCandleIndexTTL == null ? -1 : (int)nCandleIndexTTL;

            this.Gateway_OrderID = string.Empty;
            this.Gateway_AVGEXEPrice = 0;
            this.Gateway_Limit = -1;
            this.Gateway_Stop = -1;
            this.Gateway_StopLoss = -1;

            this.OrderWantedQuantity = nWantedQuantit;
            this.OrderFilledQuantity = 0;

            // Set cancel timer
            if (nCandleIndexTTL != null)
            {
                CancelOrder_Async(nCandleIndexTTL);
            }
        }
        public void Execute()
        {
            if (this.IsBuy)
            {

            }
            else
            {

            }
        }
        public void FGWResponseStopLossOrder(string strOrderID, bool bPositiveConfirmed, double dStopLossPrice, int nWantedQuantity)
        {
            this.Gateway_OrderID = strOrderID;
            this.GatewayStatusResponse = (bPositiveConfirmed || this.GatewayStatusResponse == OrderFGWResponse.PositiveConfirmed) ? OrderFGWResponse.PositiveConfirmed : OrderFGWResponse.DidntGet;
            this.Gateway_StopLoss = dStopLossPrice;
            this.OrderWantedQuantity = nWantedQuantity;
            this.ParrentShare.StopLoss = dStopLossPrice;
            Loger.ExecutionReport(this.Symbol, this, false, string.Empty);
        }
        public bool FGWResponseBuyOrder(string strOrderID, double dAVGPrice, double dLimit, int nFilledQuantitiy)
        {
            int nPreviousFilled = this.OrderFilledQuantity;

            this.ParrentShare.PositionQuantity = nFilledQuantitiy;
            this.OrderFilledQuantity = nFilledQuantitiy;
            this.Gateway_AVGEXEPrice = dAVGPrice;
            this.ParrentShare.AverageBuyPrice = dAVGPrice;
            this.Gateway_OrderID = strOrderID;
            this.Gateway_Limit = dLimit;
            Loger.ExecutionReport(this.Symbol, this, true, string.Empty);

            if (this.ParrentShare.PositionQuantity > 0)
            {
                this.ParrentShare.IsPosition = true;
                this.ParrentShare.BuyIndex = this.ParrentShare.CandleIndex;

                // Only if both 'Stop' and 'StopLoss' params are not Null - it means that the order 
                // couldent be sent as "Plus" order, and we need to take care for the stopLoss by ourselfs
                if ((this.StopLoss != null) && (this.Stop != null))
                {
                    // First Fill
                    if (nPreviousFilled == 0)
                    {
                        new Thread(() =>
                        {
                            while (!FixGatewayUtils.Sell(
                                new Order(this.Symbol, nFilledQuantitiy, null, this.StopLoss, null, false, false, null))) 
                                    { Thread.Sleep(1000); }
                        }).Start();
                    }
                    // After first fill
                    else if (nPreviousFilled < nFilledQuantitiy)
                    {
                        new Thread(() =>
                        {
                            while (!this.WasSent) { Thread.Sleep(1000); };
                            OrdersBook.Get(this.ConnectedStopLossClientOrdID).UpdateStopLossOrder(null, null);
                        }).Start();
                    }
                }


                // End of orders life
                if (this.OrderWantedQuantity == this.OrderFilledQuantity)
                {
                    OrdersBook.RemoveBuyOrder(this.ClientOrdID);
                }
            }

            return (nPreviousFilled < nFilledQuantitiy);
        }
        public void CancelOrder_Async(int? nTTL)
        {
            // If its a cancel timer - wait as TTL
            if (nTTL != null)
                new Thread(() => {while (this.ParrentShare.CandleIndex < nTTL) { Thread.Sleep(1000); };}).Start();
            

            new Thread(() =>
            { 
                while (!FixGatewayUtils.CancelOrder(this)) { Thread.Sleep(1000); }
            }).Start();    
        }
        public void UpdateStopLossOrder(double? dStopLoss, int? nQuantitiy)
        {
            while (this.GatewayStatusResponse != OrderFGWResponse.PositiveConfirmed) { Thread.Sleep(1000); }
            while (!FixGatewayUtils.UpdateStopLossOrder(this,
                                                       (int)(nQuantitiy == null ? this.ParrentShare.PositionQuantity : nQuantitiy),
                                                       (double)(dStopLoss == null ? this.StopLoss : dStopLoss))) { Thread.Sleep(1000); }
        }
        public void UpdateStopLossOrder_Async(double? dStopLoss, int? nQuantitiy)
        {
            new Thread(() =>
            {
                while (this.GatewayStatusResponse != OrderFGWResponse.PositiveConfirmed) { Thread.Sleep(1000); }
                while (!FixGatewayUtils.UpdateStopLossOrder(this,
                                                           (int)(nQuantitiy == null ? this.ParrentShare.PositionQuantity : nQuantitiy),
                                                           (double)(dStopLoss == null ? this.StopLoss : dStopLoss))) { Thread.Sleep(1000); }
            }).Start();  
        }
    }
}