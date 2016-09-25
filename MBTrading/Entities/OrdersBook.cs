using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace MBTrading.Entities
{
    public static class OrdersBook
    {
        private static ConcurrentDictionary<string, Order> OpenOrders;

        static OrdersBook()
        {
            OrdersBook.OpenOrders = new ConcurrentDictionary<string, Order>();
        }

        public static Order Get(string key)
        {
            return OrdersBook.OpenOrders[key];
        }
        public static bool TryGet(string key, out Order o)
        {
            return OrdersBook.OpenOrders.TryGetValue(key, out o);
        }
        public static bool Contains(string key)
        {
            return OrdersBook.OpenOrders.ContainsKey(key);
        }
        public static bool TryAdd(string key, Order o)
        {
            return OrdersBook.OpenOrders.TryAdd(key, o);
        }
        public static bool TryRemove(string key, out Order o)
        {
            return OrdersBook.OpenOrders.TryRemove(key,out o);
        }
        public static bool AddNewBuyOrder(Order o)
        {
            if (OrdersBook.OpenOrders.TryAdd(o.ClientOrdID, o))
            {
                o.ParrentShare.BuyOrder = o;
                return (true);
            }
            return (false);
        }
        public static bool AddNewClientStopLossOrder(Order o)
        {
            bool bOrderBook = OrdersBook.OpenOrders.TryAdd(o.ClientOrdID, o);
            bool bShareList = o.ParrentShare.StopLossOrders.TryAdd(o.ClientOrdID, o);
            return ((bOrderBook) && (bShareList));
        }
        public static bool AddNewServerStopLossOrder(string strOrderID, string strClientOrdID, string strSymbol, int nWantedQuantity, double dStopLoss)
        {
            Order oCurrOrder = new Order(strSymbol, nWantedQuantity, null, null, dStopLoss, false, false, null);
            oCurrOrder.Gateway_OrderID = strOrderID;
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
