
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
        public string OrderID;
        public double AVGEXEPrice;
        public double LimitPrice;
        public double StopLossPrice;
        public string WantedStopLosssReferencePropName;
        private string ConnectedStopLossClientOrdID;
        public int OrderWantedQuantity;
        public int OrderFilledQuantity;
        public string Symbol;
        public Share ParrentShare;
        public int CandleIndexTTL;
        public bool IsCancelSent;
        public OrderFGWResponse FGWResponse;
        public bool IsBuyOrderIndicator;
        public int Progress;

        public Order(string strClientOrdID, string strSymbol, int nWantedQuantity, string dWantedStopLosssReferencePropName, int? nCandleIndexTTL, bool bIsBuyIndicator)
        {
            this.IsCancelSent = false;
            this.FGWResponse = OrderFGWResponse.DidntGet;
            this.ConnectedStopLossClientOrdID = string.Empty;
            this.Symbol = strSymbol;
            this.CandleIndexTTL = nCandleIndexTTL == null ? -1 : (int)nCandleIndexTTL;
            this.WantedStopLosssReferencePropName = dWantedStopLosssReferencePropName;
            this.AVGEXEPrice = 0;
            this.ParrentShare = Program.SharesList[strSymbol];
            this.OrderID = string.Empty;
            this.ClientOrdID = strClientOrdID;
            this.LimitPrice = -1;
            this.StopLossPrice = -1;
            this.OrderWantedQuantity = nWantedQuantity;
            this.OrderFilledQuantity = 0;
            this.IsBuyOrderIndicator = bIsBuyIndicator;
            this.Progress = 0;
        }
        
        
        public void FGWResponseStopLossOrder(string strOrderID, bool bPositiveConfirmed, double dStopLossPrice, int nWantedQuantity)
        {
            this.OrderID = strOrderID;
            this.FGWResponse = (bPositiveConfirmed || this.FGWResponse == OrderFGWResponse.PositiveConfirmed) ? OrderFGWResponse.PositiveConfirmed : OrderFGWResponse.DidntGet;
            this.StopLossPrice = dStopLossPrice;
            this.OrderWantedQuantity = nWantedQuantity;
            this.ParrentShare.StopLoss = dStopLossPrice;
            this.Progress++;
            Loger.ExecutionReport(this.Symbol, this, false, string.Empty);
        }
        public bool FGWResponseBuyOrder(string strOrderID, double dAVGPrice, double dLimit, int nFilledQuantitiy)
        {
            int nPreviousFilled = this.OrderFilledQuantity;

            this.ParrentShare.PositionQuantity = nFilledQuantitiy;
            this.OrderFilledQuantity = nFilledQuantitiy;
            this.AVGEXEPrice = dAVGPrice;
            this.ParrentShare.BuyPrice = dAVGPrice;
            this.OrderID = strOrderID;  
            this.LimitPrice = dLimit;
            this.Progress++;
            Loger.ExecutionReport(this.Symbol, this, true, string.Empty);

            if (this.ParrentShare.PositionQuantity > 0)
            {
                this.ParrentShare.IsPosition = true;
                this.ParrentShare.BuyIndex = this.ParrentShare.CandleIndex;

                // First Fill
                if (nPreviousFilled == 0)
                {
                    new Thread(() =>
                    {
                        while (!FixGatewayUtils.NewStopLossOrder(this.Symbol,
                                                                 this.ParrentShare.ChangingStopPirce.GetStopLossPriceByName(this.WantedStopLosssReferencePropName),
                                                                 nFilledQuantitiy, 
                                                                 out this.ConnectedStopLossClientOrdID)) { Thread.Sleep(1000); }
                    }).Start();
                }
                // After first fill
                else if (nPreviousFilled < nFilledQuantitiy)
                {
                    new Thread(() =>
                    {
                        while (this.ConnectedStopLossClientOrdID == string.Empty) { Thread.Sleep(1000); };
                        OrdersBook.OpenOrders[this.ConnectedStopLossClientOrdID].UpdateStopLossOrder_ParentThread(null,null);
                    }).Start();
                }

                // End of orders life
                if (this.OrderWantedQuantity == this.OrderFilledQuantity)
                {
                    OrdersBook.RemoveBuyOrder(this.ClientOrdID);
                }
            }

            return (nPreviousFilled < nFilledQuantitiy);
        }
        public void CancelOrder_NewThread()
        {
            this.IsCancelSent = true;
            new Thread(() =>
            { 
                while (!FixGatewayUtils.CancelOrder(this.Symbol, this.ClientOrdID, this.IsBuyOrderIndicator)) { Thread.Sleep(1000); } 
            }).Start();    
        }
        public void UpdateStopLossOrder_ParentThread(double? dStop, int? nQuantitiy)
        {
            while (this.FGWResponse != OrderFGWResponse.PositiveConfirmed) { Thread.Sleep(1000); }
            while (!FixGatewayUtils.UpdateStopLossOrder(this.Symbol,
                                                        this.ClientOrdID,
                                                        dStop == null ? this.StopLossPrice: (double)dStop,
                                                        nQuantitiy == null ? this.ParrentShare.PositionQuantity : (int)nQuantitiy)) { Thread.Sleep(1000); }
        }
        public void UpdateStopLossOrder_NewThread(double? dStop, int? nQuantitiy)
        {
            new Thread(() =>
            {
                while (this.FGWResponse != OrderFGWResponse.PositiveConfirmed) { Thread.Sleep(1000); }
                while (!FixGatewayUtils.UpdateStopLossOrder(this.Symbol,
                                                            this.ClientOrdID,
                                                            dStop == null ? this.StopLossPrice : (double)dStop,
                                                            nQuantitiy == null ? this.ParrentShare.PositionQuantity : (int)nQuantitiy)) { Thread.Sleep(1000); }
            }).Start();  
        }
    }
}