using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace MBTrading.Entities
{
    public static class OrdersBook
    {
        public static ConcurrentDictionary<string, Order> OpenOrders;

        static OrdersBook()
        {
            OrdersBook.OpenOrders = new ConcurrentDictionary<string, Order>();
        }

        public static bool AddNewBuyOrder(string strClientOrdID, string strSymbol, int nWantedQuantity, string strStopLossReferencePropName, int? nCandelIndexTTL)
        {
            Order oCurrOrder = new Order(strClientOrdID, strSymbol, nWantedQuantity, strStopLossReferencePropName, nCandelIndexTTL, true);
            if (OrdersBook.OpenOrders.TryAdd(strClientOrdID, oCurrOrder))
            {
                oCurrOrder.ParrentShare.BuyOrder = oCurrOrder;
                return (true);
            }
            return (false);
        }
        public static bool AddNewClientStopLossOrder(string strClientOrdID, string strSymbol, int nWantedQuantity, string strStopLossReferencePropName)
        {
            Order oCurrOrder = new Order(strClientOrdID, strSymbol, nWantedQuantity, string.Empty, null, false);
            bool bOrderBook = OrdersBook.OpenOrders.TryAdd(strClientOrdID, oCurrOrder);
            bool bShareList = oCurrOrder.ParrentShare.StopLossOrders.TryAdd(strClientOrdID, oCurrOrder);
            return ((bOrderBook) && (bShareList));
        }
        public static bool AddNewServerStopLossOrder(string strOrderID, string strClientOrdID, string strSymbol, int nWantedQuantity)
        {
            Order oCurrOrder = new Order(strClientOrdID, strSymbol, nWantedQuantity, string.Empty, null, false);
            oCurrOrder.OrderID = strOrderID;
            bool bOrderBook = OrdersBook.OpenOrders.TryAdd(strOrderID, oCurrOrder);
            bool bShareList = oCurrOrder.ParrentShare.StopLossOrders.TryAdd(strOrderID, oCurrOrder);
            return ((bOrderBook) && (bShareList));
        }


        public static void RemoveBuyOrder(string strClientOrdID)
        {
            Order oOrderToRemove = null;
            OrdersBook.OpenOrders.TryRemove(strClientOrdID, out oOrderToRemove);
            oOrderToRemove.ParrentShare.BuyOrder = null;
            Loger.ExecutionReport(oOrderToRemove.ParrentShare.Symbol, null, true, string.Format("Cid:{0} Oid:{1} Removed from book", oOrderToRemove.ClientOrdID, oOrderToRemove.OrderID));
        }
        public static void RemoveStopLossOrder(string strOrderID)
        {
            Order oOrderToRemove = null;
            OrdersBook.OpenOrders.TryRemove(strOrderID, out oOrderToRemove);
            oOrderToRemove.ParrentShare.StopLossOrders.TryRemove(strOrderID, out oOrderToRemove);
        }
        public static void CancelOrderNotFoundHandler(string strSymbol, bool bCancelBuyOrderIndicator)
        {
            // TODO: האם המנייה בכלל בפוזיציה או לא
            // 
        }
    }
}
