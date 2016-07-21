using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading;

namespace MBTrading.Utils
{
    public static class PushServer
    {
        // Incoming data from the client.
        public static Socket handler;
        private static long _lockFlag = 0;
        private static long _T_Flag = 1;
        private static long _F_Flag = 0;

        public static void StartListening()
        {
            // Establish the local endpoint for the socket.
            // Dns.GetHostName returns the name of the 
            // host running the application.
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(Consts.WEBTRADING_SERVER_IP), Consts.WEBTRADING_SERVER_PORT);
            
            // Create a TCP/IP socket.
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and 
            // listen for incoming connections.
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(1);

                // Start listening for connections.

                Console.WriteLine("Waiting for WebTrading to get UP...\n");
                
                // Program is suspended while waiting for an incoming connection.
                handler = listener.Accept();
            }
            catch (Exception e)
            {
                if (handler != null)
                {
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
                Console.WriteLine(e.ToString());
            }
        }
        public static string RealtimeMessage(double dBallance, double dTotalAccountProfit, double dTotalAccountLoss, ConcurrentDictionary<string, Share> lstAllShares)
        {
            StringBuilder stbRealtimeMessage = new StringBuilder(string.Format("{{\"Ballance\" : {0}, \"TotalProfit\" : {1}, \"TotalLoss\" : {2}, \"Shares\" : [ ", dBallance, dTotalAccountProfit, dTotalAccountLoss));
            
            // Run over all shares and make BSON       
            foreach (Share sCurrShare in lstAllShares.Values)
            {
                stbRealtimeMessage.Append(string.Format("{{ \"Symbol\" : \"{0}\", \"LastPrice\" : {1},\"IsPosition\" : {2}, \"OffLineIsPosition\" : {3}, \"CurrPL\" : {4}, \"TotalPL\" : {5}, \"StopLimit\" : {6}, \"BuyPrice\" : {7}, \"StopLoss\" : {8} }},",
                    sCurrShare.Symbol,
                    sCurrShare.CandlesList.CurrPrice,
                    sCurrShare.IsPosition.ToString().ToLower(),
                    sCurrShare.OffLineIsPosition.ToString().ToLower(),
                    sCurrShare.CurrPL,
                    sCurrShare.TotalPL,
                    sCurrShare.BuyOrder == null ? 0 : sCurrShare.BuyOrder.LimitPrice,
                    sCurrShare.AverageBuyPrice,
                    sCurrShare.StopLossOrders.Count == 0 && !sCurrShare.OffLineIsPosition ? 0 : sCurrShare.StopLoss));
            }

            stbRealtimeMessage.Remove(stbRealtimeMessage.Length - 1, 1);
            stbRealtimeMessage.Append("]}");

            // Update document
            return (stbRealtimeMessage.ToString());
        }

        public static void SendTCPMessage(string strMSG)
        {
            if (handler != null)
            {
                // If there is no other thread preforming this method
                if ((Interlocked.CompareExchange(ref _lockFlag, _T_Flag, _F_Flag) == _F_Flag) && (!FixGatewayUtils.LogonIndicator))
                {
                    try
                    {
                        byte[] arr = Encoding.ASCII.GetBytes(strMSG);
                        handler.Send(arr, arr.Length, SocketFlags.None);
                    }
                    catch
                    {
                        handler = null;
                        new Thread(StartListening).Start();
                    }
                    finally
                    {
                        Interlocked.Decrement(ref _lockFlag);
                    }
                }
            }
        }
    }
}
