using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MBTrading.Utils;
using MBTrading.Entities;
using System.Net;
using System.IO;

namespace MBTrading
{
    public static class OandaGatewayUtils
    {
        // Static Data Members        
        public static string TokenHeader            = "Authorization: Bearer 20e179b8ebcd8a1e5fc19429785c04a7-ea3f2a34f65ca8772bd4e2e938572cb8";
        public static string ContentTypeHeader      = "application/json";
        public static string AccountURL             = "https://api-fxpractice.oanda.com/v3/accounts/101-004-4411535-001/{0}";



        static OandaGatewayUtils()
        {
          
        }

        public static bool Buy(string strSymbol, int nPositionQuantity, bool bLongPosition, double dLimit, double dStopLoss)
        {
            nPositionQuantity = bLongPosition ? nPositionQuantity : 0 - nPositionQuantity;
            string strInstrument = strSymbol.Replace('/', '_');
            Loger.Messages(String.Format("Client   :   {0} BuyLimit ({1})   {2}  **{3}**", strSymbol, nPositionQuantity, dLimit, dStopLoss), true);

            try
            {
                // Get MBQuateAPI connecting details
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format(AccountURL, "orders"));
                request.ContentType = ContentTypeHeader;
                request.Headers.Add(TokenHeader);
                request.Method = "POST";
                request.KeepAlive = false;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls |
                                                       SecurityProtocolType.Tls11 |
                                                       SecurityProtocolType.Tls12 |
                                                       SecurityProtocolType.Ssl3;

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    string json = string.Format("{{\"order\": {{\"instrument\" : \"{0}\",\"type\" : \"LIMIT\",\"positionFill\" : \"DEFAULT\",\"timeInForce\" : \"GTC\",\"units\" : \"{1}\",\"price\" : \"{2}\",\"stopLossOnFill\": {{\"timeInForce\": \"GTC\", \"price\": \"{3}\"}}}}}}", 
                        strInstrument, 
                        nPositionQuantity,
                        dLimit,
                        dStopLoss);

                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                using (WebResponse response = request.GetResponse())
                {
                    // Read response xml
                    using (StreamReader responseReader = new StreamReader(response.GetResponseStream()))
                    {
                        Loger.Messages(String.Format("Oanda   :    {0}", responseReader.ReadToEnd()), true);
                    }
                }
            }
            catch (Exception e)
            {
                Loger.ErrorLog(e);
                return (false);
            }

            return (true);
        }
        public static bool Sell(string strSymbol, int nPositionQuantity, bool bLongPosition)
        {
            nPositionQuantity = bLongPosition ? 0 - nPositionQuantity : nPositionQuantity;
            string strInstrument = strSymbol.Replace('/', '_');
            Loger.Messages(String.Format("Client   :   {0} MarketSell ({1})", strSymbol, nPositionQuantity), true);

            try
            {
                // Get MBQuateAPI connecting details
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format(AccountURL, "orders"));
                request.ContentType = ContentTypeHeader;
                request.Headers.Add(TokenHeader);
                request.Method = "POST";
                request.KeepAlive = false;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls |
                                                       SecurityProtocolType.Tls11 |
                                                       SecurityProtocolType.Tls12 |
                                                       SecurityProtocolType.Ssl3;

                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    string json = string.Format("{{\"order\": {{\"instrument\" : \"{0}\",\"type\" : \"MARKET\",\"positionFill\" : \"DEFAULT\",\"timeInForce\" : \"FOK\",\"units\" : \"{1}\"}}}}", strInstrument, nPositionQuantity);
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    streamWriter.Close();
                }

                using (WebResponse response = request.GetResponse())
                {
                    // Read response xml
                    using (StreamReader responseReader = new StreamReader(response.GetResponseStream()))
                    {
                        Loger.Messages(String.Format("Oanda   :    {0}", responseReader.ReadToEnd()), true);
                    }
                }
            }
            catch (Exception e)
            {
                Loger.ErrorLog(e);
                return (false);
            }

            return (true);
        }
    }
}