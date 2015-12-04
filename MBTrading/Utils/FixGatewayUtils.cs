using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MBTrading.Utils;
using MBTrading.Entities;

namespace MBTrading
{
    public static class FixGatewayUtils
    {
        // Static Data Members        
        public  static Thread                                   MBTradingListenerLoopThread     = null;
        public  static Thread                                   MBTradingHeartBeatThread        = null;
        public  static Thread                                   MBTradingESTRolloverThread      = null;
        public  static Thread                                   MBTradingSequenceResetThread    = null;
        private static TcpClient                                tcpClient                       = null;
        private static NetworkStream                            networkStream                   = null;                     // TODO: SSL_STREAM
        private static string                                   SessionToken                    = string.Empty;
        public  static Integer                                  Sequence                        = null;
        public  static bool                                     LogonIndicator                  = false;
        public  static bool                                     OperationalTime                 = false;
        public  static bool                                     SystemProblem = false;
        public  static ConcurrentDictionary<int, string>        MessageHistoryList              = null;
        public  static int                                      LastGatewaySequence;
        public  static DateTime                                 LastHB;
        private static long                                     _lockFlag                   = 0;
        private static long                                     _T_Flag                     = 1;
        private static long                                     _F_Flag                     = 0;
        private static string                                   Message                     = "8=FIX.4.49={0}{1}";      // 9    : body{1}{2} Length
                                                                                                                        // {1}  : Body - RequiredValues + body
        private static string                                   RequiredValues = "35={0}49={1}56={2}34={3}52={4}";      // 35   : MsgType
                                                                                                                        // 49   : Sender
                                                                                                                        // 56   : Target
                                                                                                                        // 34   : Sequence
                                                                                                                        // 52   : Sending Time (UTC)


        static FixGatewayUtils()
        {
            FixGatewayUtils.Sequence = new Integer(0);
            FixGatewayUtils.LastGatewaySequence = 0;
            FixGatewayUtils.MessageHistoryList = new ConcurrentDictionary<int, string>();
        }

        // Static Methods - Quote API
        public static bool ConnectFixGatewayTCP()
        {
            try
            {
                // Initiate TCP and network stream
                FixGatewayUtils.tcpClient = new TcpClient();
                FixGatewayUtils.tcpClient.Connect(Consts.FixGW_IP, Consts.FixGW_Port);
                FixGatewayUtils.networkStream = tcpClient.GetStream();
                FixGatewayUtils.networkStream.ReadTimeout = 60000;
                return (true);

                // TODO: SSL
                // TODO: MBTradingUtils.networkStream.AuthenticateAsClient(FixGatewayIP);
            }
            catch (Exception e)
            {
                Loger.ErrorLog(e);
                return (false);
            }
        }
        public static bool DisConnectFixGatewayTCP()
        {
            try
            {
                // Close all TCP and networkStreams
                if (FixGatewayUtils.tcpClient != null)
                    FixGatewayUtils.tcpClient.Close();
                FixGatewayUtils.tcpClient = null;
                FixGatewayUtils.networkStream = null;
                return (true);
            }
            catch (Exception e)
            {                
                Loger.ErrorLog(e);
                return (false);
            }
        }
        public static void LogOnMBTFixGateway(bool bResetSequenceFlag, bool bResetSequence)
        {
            // If there is no other thread preforming this method
            if ((Interlocked.CompareExchange(ref _lockFlag, _T_Flag, _F_Flag) == _F_Flag) &&  (!FixGatewayUtils.LogonIndicator))
            {
                // DebugPrint
                Loger.Sequence("ON  - " + bResetSequenceFlag.ToString());

                // Try to connect TCP Level
                FixGatewayUtils.DisConnectFixGatewayTCP();
                while (!FixGatewayUtils.ConnectFixGatewayTCP()) { Thread.Sleep(90000); FixGatewayUtils.DisConnectFixGatewayTCP(); }

                // Initialize the MBTFixGateway connection string
                string strResetSequenceTag = string.Empty;
                if ((bResetSequenceFlag) || (FixGatewayUtils.SystemProblem))
                {                   
                    strResetSequenceTag = "141=Y";
                }
                if ((bResetSequence) || (FixGatewayUtils.SystemProblem))
                {
                    FixGatewayUtils.CleanSequence();
                }
                string strSequence = FixGatewayUtils.GetSecureSequence().ToString();
                string strRequiredValuesFormated = string.Format(RequiredValues, 'A', Consts.FixGW_SenderCompID, Consts.FixGW_TargetCompID, strSequence, DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss"));
                string strLogonStringFormated = string.Format("98=0{0}108=0347=554_H1554={1}", strResetSequenceTag, Consts.FixGW_Pass);
                string strBodyStringFormated = string.Format("{0}{1}", strRequiredValuesFormated, strLogonStringFormated);
                string strMessageFormated = string.Format(Message, strBodyStringFormated.Length, strBodyStringFormated);
                strMessageFormated = FixGatewayUtils.CalculateCheckSum(strMessageFormated);

                try
                {
                    // Login to server
                    byte[] loginStream = System.Text.Encoding.ASCII.GetBytes(strMessageFormated);
                    networkStream.Write(loginStream, 0, loginStream.Length);
                    Loger.Messages(strMessageFormated);
                    networkStream.Flush();
                }
                catch (Exception e)
                {
                    if (bResetSequence)
                    {
                        FixGatewayUtils.CleanSequence();
                    }
                    Loger.ErrorLog(e);
                }
                finally 
                { 
                    FixGatewayUtils.AddToHistoryLog(strSequence, strMessageFormated);
                    
                    // Free the lock
                    Thread.Sleep(90000);
                    Interlocked.Decrement(ref _lockFlag);
                    if ((!FixGatewayUtils.LogonIndicator) && (!FixGatewayUtils.OperationalTime))
                        FixGatewayUtils.LogOnMBTFixGateway(bResetSequenceFlag,bResetSequence);
                }
            }
        }
        public static void LogOutMBTFixGateway()
        {
            // DebugPrint
            Loger.Sequence("OFF");
            FixGatewayUtils.LogonIndicator = false;

            // Initialize the logout MBTFixGateway string         
            string strSequence = FixGatewayUtils.GetSecureSequence().ToString();
            string strBodyStringFormated = string.Format(RequiredValues, '5', Consts.FixGW_SenderCompID, Consts.FixGW_TargetCompID, strSequence, DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss"));
            string strMessageFormated = string.Format(Message, strBodyStringFormated.Length, strBodyStringFormated);
            strMessageFormated = FixGatewayUtils.CalculateCheckSum(strMessageFormated);

            try
            {
                // Login to server
                byte[] loginStream = System.Text.Encoding.ASCII.GetBytes(strMessageFormated);
                networkStream.Write(loginStream, 0, loginStream.Length);
                Loger.Messages(strMessageFormated);
                networkStream.Flush();

                // Disconnecting TCP Level
                FixGatewayUtils.DisConnectFixGatewayTCP();
            }
            catch (Exception e)
            {
                Loger.ErrorLog(e);
            }
            finally { FixGatewayUtils.AddToHistoryLog(strSequence, strMessageFormated); }
        }
        public static void CleanSequence()
        {
            // Initiate all Sequence companents
            FixGatewayUtils.Sequence = new Integer(0);
            FixGatewayUtils.LastGatewaySequence = 0;
            FixGatewayUtils.MessageHistoryList.Clear();
        }
        public static void CheckIfConnectionError(Exception e)
        {
            if ((e.Message.Contains("Didn't received Gateway HB")) ||
                (e.Message.Contains("An established connection was aborted by the software in your host machine")) || 
                (e.Message.Contains("No connection could be made because the target machine actively refused it")) ||
                (e.Message.Contains("An existing connection was forcibly closed by the remote host")) ||
                (e.Message.Contains("Cannot access a disposed object")) ||
                (e.Message.Contains("A connection attempt failed because the connected party did not properly respond after a period of time")))
            {
                FixGatewayUtils.LogonIndicator = false;

                // DebugPrint
                Loger.Sequence(FixGatewayUtils.OperationalTime.ToString() + e.Message);

                if (FixGatewayUtils.OperationalTime)
                {
                    // Sends ConnectionError mail and try to Reconnect
                    Mail.SendMBTreadingMail("ConnectionError");
                    new Thread(() => FixGatewayUtils.LogOnMBTFixGateway(false, false)).Start();
                }
            }
        }
        
        public static void ESTRollover()
        {
            // Initiailzing Rollover and ListenerLoop Threads
            FixGatewayUtils.MBTradingESTRolloverThread = Thread.CurrentThread;
            FixGatewayUtils.MBTradingESTRolloverThread.Priority = ThreadPriority.Highest;
            FixGatewayUtils.MBTradingListenerLoopThread = new Thread(FixGatewayUtils.ListenerLoop);
            FixGatewayUtils.MBTradingListenerLoopThread.Start();
            Thread.Sleep(10000);

            // If it's weekend - wait till Trade openning time
            while (Time.IsWeekendNow()) { Thread.Sleep(60000); }
         
            // Initiateing Heart Beats and First Logon!
            FixGatewayUtils.MBTradingHeartBeatThread = new Thread(FixGatewayUtils.HeartBeatLoop);
            FixGatewayUtils.MBTradingHeartBeatThread.Priority = ThreadPriority.Highest;
            FixGatewayUtils.MBTradingHeartBeatThread.Start();
            FixGatewayUtils.LogOnMBTFixGateway(true, true);

            // If SequenceReset and Rollover are in diffarent time - Initializing also SequenceReset Thread
            if (Consts.SequenceResetTime != Consts.RolloverTime)
            {
                FixGatewayUtils.MBTradingSequenceResetThread = new Thread(FixGatewayUtils.SEQRollover);
                FixGatewayUtils.MBTradingSequenceResetThread.Priority = ThreadPriority.Highest;
                FixGatewayUtils.MBTradingSequenceResetThread.Start();
            }

            // Wait for the first Rollover
            TimeSpan tToWait = Time.ESTToWait(Consts.RolloverTime - 1, 59);
            Thread.Sleep(tToWait);

            while (Program.IsProgramAlive)
            {
                // Logout 
                // DebugPrint
                Loger.Sequence("EST OFF");
                Consts.LoadConsts();
                foreach (Share sCurrShare in Program.SharesList.Values) { sCurrShare.UpdateShareConsts(); }
                FixGatewayUtils.OperationalTime = false;
                FixGatewayUtils.LogOutMBTFixGateway();

                // If today is Friday - Wait till sunday to logon again
                if (DateTime.Now.DayOfWeek == DayOfWeek.Friday)
                {
                    tToWait = Time.ESTToWait(Consts.RolloverTime, 6);
                    tToWait = tToWait.Add(new TimeSpan(2, 0, 0, 0));
                    Thread.Sleep(tToWait);
                }
                else
                {
                    Thread.Sleep(420000);
                }

                // Logon
                Loger.Sequence("EST ON");
                FixGatewayUtils.LogOnMBTFixGateway(true, true);
                Thread.Sleep(10000);

                // Wait for Rollover time
                tToWait = Time.ESTToWait(Consts.RolloverTime - 1, 59);
                Thread.Sleep(tToWait);
            }
        }
        public static void SEQRollover()
        {
            // Wait for the first SequenceRollover
            TimeSpan tToWait = Time.ESTToWait(Consts.SequenceResetTime - 1, 59);
            Thread.Sleep(tToWait);

            while ((Time.EST.DayOfWeek == DayOfWeek.Friday) || (Time.EST.DayOfWeek == DayOfWeek.Saturday)) { Thread.Sleep(Time.ESTToWait(Consts.SequenceResetTime - 1, 59)); }  

            while (Program.IsProgramAlive)
            {
                // Logout 
                Loger.Sequence("SEQ OFF");
                FixGatewayUtils.OperationalTime = false;
                FixGatewayUtils.LogOutMBTFixGateway();

                Thread.Sleep(Time.ESTToWait(Consts.SequenceResetTime, 2));

                // Logon
                Loger.Sequence("SEQ ON");
                FixGatewayUtils.LogOnMBTFixGateway(true, true);
                Thread.Sleep(10000);

                tToWait = Time.ESTToWait(Consts.SequenceResetTime - 1, 59);
                Thread.Sleep(tToWait);
                while ((Time.EST.DayOfWeek == DayOfWeek.Friday) || 
                       (Time.EST.DayOfWeek == DayOfWeek.Saturday)) 
                {
                    Thread.Sleep(Time.ESTToWait(Consts.SequenceResetTime - 1, 59)); 
                }  
            }
        }
        public static void SendHeartbeat(string strTestReqID)
        {
            string strSequence = FixGatewayUtils.GetSecureSequence().ToString();
            string strDateTimeUTC = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss");
            string strRequiredValuesFormated = string.Format(RequiredValues, "0", Consts.FixGW_SenderCompID, Consts.FixGW_TargetCompID, strSequence, strDateTimeUTC);
            string strNewRequestFormated = strTestReqID == null ? string.Empty : string.Format("112={0}", strTestReqID);
            string strBodyStringFormated = string.Format("{0}{1}", strRequiredValuesFormated, strNewRequestFormated);
            string strMessageFormated = string.Format(Message, strBodyStringFormated.Length, strBodyStringFormated);
            strMessageFormated = FixGatewayUtils.CalculateCheckSum(strMessageFormated);
            
            try
            {
                // Send request
                byte[] loginStream = System.Text.Encoding.ASCII.GetBytes(strMessageFormated);
                networkStream.Write(loginStream, 0, loginStream.Length);
                Loger.Messages(strMessageFormated);
                networkStream.Flush();
            }
            catch (Exception e)
            {
                Loger.ErrorLog(e);
                FixGatewayUtils.CheckIfConnectionError(e);
            }
            finally { FixGatewayUtils.AddToHistoryLog(strSequence, strMessageFormated); }
        }
        public static void SequenceReset(bool bGapFillMode, int nGapStartSequence, int nNextGapSequence)
        {
            string strMessageHeaderSequence;
            char cGapFillMode;
            int nNextSequence;

            if (bGapFillMode)
            {
                cGapFillMode = 'Y';
                strMessageHeaderSequence = nGapStartSequence.ToString();
                nNextSequence = nNextGapSequence;
            }
            else
            {
                cGapFillMode = 'N';
                strMessageHeaderSequence = FixGatewayUtils.GetSecureSequence().ToString();
                nNextSequence = FixGatewayUtils.GetSecureSequence();
            }

            string strDateTimeUTC = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss");
            string strRequiredValuesFormated = string.Format(RequiredValues, "4", Consts.FixGW_SenderCompID, Consts.FixGW_TargetCompID, strMessageHeaderSequence, strDateTimeUTC);
            string strNewRequestFormated = string.Format("123={0}36={1}", cGapFillMode, nNextSequence);
            string strBodyStringFormated = string.Format("{0}{1}", strRequiredValuesFormated, strNewRequestFormated);
            string strMessageFormated = string.Format(Message, strBodyStringFormated.Length, strBodyStringFormated);
            strMessageFormated = FixGatewayUtils.CalculateCheckSum(strMessageFormated);
            
            try
            {
                if (!bGapFillMode)
                {
                    // Send request
                    byte[] loginStream = System.Text.Encoding.ASCII.GetBytes(strMessageFormated);
                    networkStream.Write(loginStream, 0, loginStream.Length);
                    Loger.Messages(strMessageFormated);
                    networkStream.Flush();
                }
            }
            catch (Exception e)
            {
                Loger.ErrorLog(e);
            }
            finally { FixGatewayUtils.AddToHistoryLog(strMessageHeaderSequence, strMessageFormated); }
        }
        public static void ResendRequest(int nBeginSeqNo, int nEndSeqNo)
        {
            string strSequence = FixGatewayUtils.GetSecureSequence().ToString();
            string strDateTimeUTC = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss");
            string strRequiredValuesFormated = string.Format(RequiredValues, "2", Consts.FixGW_SenderCompID, Consts.FixGW_TargetCompID, strSequence, strDateTimeUTC);
            string strNewRequestFormated = string.Format("7={0}16={1}", nBeginSeqNo, nEndSeqNo);
            string strBodyStringFormated = string.Format("{0}{1}", strRequiredValuesFormated, strNewRequestFormated);
            string strMessageFormated = string.Format(Message, strBodyStringFormated.Length, strBodyStringFormated);
            strMessageFormated = FixGatewayUtils.CalculateCheckSum(strMessageFormated);

            try
            {
                // Send request
                byte[] loginStream = System.Text.Encoding.ASCII.GetBytes(strMessageFormated);
                networkStream.Write(loginStream, 0, loginStream.Length);
                Loger.Messages(strMessageFormated);
                networkStream.Flush();
            }
            catch (Exception e)
            {
                Loger.ErrorLog(e);
            }
            finally { FixGatewayUtils.AddToHistoryLog(strSequence, strMessageFormated); }
        }
        public static void BuyLimitPlusStopMarket(string strSymbol, double dLimit, double dStop, int nQuantity)
        {
            string strSequence = FixGatewayUtils.GetSecureSequence().ToString();
            string strDateTimeUTC = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss");
            string strRequiredValuesFormated = string.Format(RequiredValues, "D", Consts.FixGW_SenderCompID, Consts.FixGW_TargetCompID, strSequence, strDateTimeUTC);
            string strNewOrderFormated = string.Format("1={0}11={1}18=s21={2}38={3}40={4}44={5}47={6}54={7}55={8}59={9}60={10}99={11}100={12}529=1553={13}", Consts.Account_No, strSequence + strDateTimeUTC, 1, nQuantity, 2, dLimit, 'I', 1, strSymbol, 1, strDateTimeUTC, dStop, "MBTX", Consts.Account_UserName);
            string strBodyStringFormated = string.Format("{0}{1}", strRequiredValuesFormated, strNewOrderFormated);
            string strMessageFormated = string.Format(Message, strBodyStringFormated.Length, strBodyStringFormated);
            strMessageFormated = FixGatewayUtils.CalculateCheckSum(strMessageFormated);
            
            try
            {
                // Send request
                byte[] loginStream = System.Text.Encoding.ASCII.GetBytes(strMessageFormated);
                networkStream.Write(loginStream, 0, loginStream.Length);
                Loger.Messages(strMessageFormated);
                networkStream.Flush();
            }
            catch (Exception e)
            {
                Loger.ErrorLog(e);
            }
            finally { FixGatewayUtils.AddToHistoryLog(strSequence, strMessageFormated); }
        }
        public static bool BuyStopLimitPlusTrailing(string strSymbol, double dLimit, double dStop, double dTrailingInterval, int nQuantity, string strStopLossReferencePropName, int? nCandelIndexTTL, string strBuyCouse)
        {
            if (FixGatewayUtils.LogonIndicator)
            {
                string strSequence = FixGatewayUtils.GetSecureSequence().ToString();
                string strDateTimeUTC = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss");
                string strClientOrdID = string.Format("{0}{1}", strSequence, strDateTimeUTC);
                string strRequiredValuesFormated = string.Format(RequiredValues, "D", Consts.FixGW_SenderCompID, Consts.FixGW_TargetCompID, strSequence, strDateTimeUTC);
                string strNewOrderFormated = string.Format("1={0}11={1}18=p21={2}38={3}40={4}44={5}47={6}54={7}55={8}59={9}60={10}99={11}211={12}100={13}529=1553={14}", Consts.Account_No, strClientOrdID, 1, nQuantity, 4, dLimit, 'I', 1, strSymbol, 1, strDateTimeUTC, dStop, dTrailingInterval, "MBTX", Consts.Account_UserName);
                string strBodyStringFormated = string.Format("{0}{1}", strRequiredValuesFormated, strNewOrderFormated);
                string strMessageFormated = string.Format(Message, strBodyStringFormated.Length, strBodyStringFormated);
                strMessageFormated = FixGatewayUtils.CalculateCheckSum(strMessageFormated);

                try
                {
                    // Send request
                    byte[] loginStream = System.Text.Encoding.ASCII.GetBytes(strMessageFormated);
                    networkStream.Write(loginStream, 0, loginStream.Length);
                    Loger.Messages(strMessageFormated);
                    networkStream.Flush();
                }
                catch (Exception e)
                {
                    Program.SharesList[strSymbol].IsBuyOrderSentOrExecuted = false;
                    Loger.ErrorLog(e);
                    return (false);
                }
                finally { FixGatewayUtils.AddToHistoryLog(strSequence, strMessageFormated); }

                Program.SharesList[strSymbol].IsBuyOrderSentOrExecuted = true;
                OrdersBook.AddNewBuyOrder(strClientOrdID, strSymbol, nQuantity, strStopLossReferencePropName, nCandelIndexTTL);
                return (true);
            }

            Program.SharesList[strSymbol].IsBuyOrderSentOrExecuted = false;
            Loger.ExecutionReport(strSymbol, null, true, "BuyStopLimitPlusTrailing - LogonIndicator == False");
            return (false);
        }
        public static bool BuyStopLimit(string strSymbol, double dLimit, double dStop, int nQuantity, string strStopLossReferencePropName, int? nCandelIndexTTL, string strBuyCouse)
        {
            if (FixGatewayUtils.LogonIndicator)
            {
                string strSequence = FixGatewayUtils.GetSecureSequence().ToString();
                string strDateTimeUTC = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss");
                string strClientOrdID = string.Format("{0}{1}", strSequence, strDateTimeUTC);
                string strRequiredValuesFormated = string.Format(RequiredValues, "D", Consts.FixGW_SenderCompID, Consts.FixGW_TargetCompID, strSequence, strDateTimeUTC);
                string strNewOrderFormated = string.Format("1={0}11={1}21={2}38={3}40={4}44={5}47={6}54={7}55={8}59={9}60={10}99={11}100={12}553={13}", Consts.Account_No, strClientOrdID, 1, nQuantity, 4, dLimit, 'I', 1, strSymbol, 1, strDateTimeUTC, dStop, "MBTX", Consts.Account_UserName);
                string strBodyStringFormated = string.Format("{0}{1}", strRequiredValuesFormated, strNewOrderFormated);
                string strMessageFormated = string.Format(Message, strBodyStringFormated.Length, strBodyStringFormated);
                strMessageFormated = FixGatewayUtils.CalculateCheckSum(strMessageFormated);

                try
                {
                    // Send request
                    byte[] loginStream = System.Text.Encoding.ASCII.GetBytes(strMessageFormated);
                    networkStream.Write(loginStream, 0, loginStream.Length);
                    Loger.ExecutionReport(strSymbol, null, true, string.Format("BuyStopLimit - Cid:{0}      TriggeredBy: {1}", strClientOrdID, strBuyCouse));
                    Loger.Messages(strMessageFormated);
                    networkStream.Flush();
                }
                catch (Exception e)
                {
                    Loger.ExecutionReport(strSymbol, null, true, "BuyStopLimit - Exception");
                    Loger.ErrorLog(e);
                    Program.SharesList[strSymbol].IsBuyOrderSentOrExecuted = false;
                    return (false);
                }
                finally { FixGatewayUtils.AddToHistoryLog(strSequence, strMessageFormated); }

                Program.SharesList[strSymbol].IsBuyOrderSentOrExecuted = true;
                OrdersBook.AddNewBuyOrder(strClientOrdID, strSymbol, nQuantity, strStopLossReferencePropName, nCandelIndexTTL);
                return (true);
            }

            Program.SharesList[strSymbol].IsBuyOrderSentOrExecuted = false;
            Loger.ExecutionReport(strSymbol, null, true, "BuyStopLimit - LogonIndicator == False");
            return (false);
        }
        public static bool SellLimit(string strSymbol, double dLimit, int nQuantity)
        {         
            string strSequence = FixGatewayUtils.GetSecureSequence().ToString();
            string strDateTimeUTC = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss");
            string strRequiredValuesFormated = string.Format(RequiredValues, "D", Consts.FixGW_SenderCompID, Consts.FixGW_TargetCompID, strSequence, strDateTimeUTC);
            string strNewOrderFormated = string.Format("1={0}11={1}21={2}38={3}40={4}44={5}47={6}54={7}55={8}59={9}60={10}100={11}553={12}", Consts.Account_No, strSequence + strDateTimeUTC, 1, nQuantity, 2, dLimit, 'I', 2, strSymbol, 1, strDateTimeUTC, "MBTX", Consts.Account_UserName);
            string strBodyStringFormated = string.Format("{0}{1}", strRequiredValuesFormated, strNewOrderFormated);
            string strMessageFormated = string.Format(Message, strBodyStringFormated.Length, strBodyStringFormated);
            strMessageFormated = FixGatewayUtils.CalculateCheckSum(strMessageFormated);

            try
            {
                // Send request
                byte[] loginStream = System.Text.Encoding.ASCII.GetBytes(strMessageFormated);
                networkStream.Write(loginStream, 0, loginStream.Length);
                Loger.Messages(strMessageFormated);
                networkStream.Flush();
                return (true);
            }
            catch (Exception e)
            {
                Loger.ErrorLog(e);
                return (false);
            }
            finally { FixGatewayUtils.AddToHistoryLog(strSequence, strMessageFormated); }
        }
        public static bool SellMarket(string strSymbol, int nQuantity)
        {
            if (FixGatewayUtils.LogonIndicator)
            {
                string strSequence = FixGatewayUtils.GetSecureSequence().ToString();
                string strDateTimeUTC = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss");
                string strRequiredValuesFormated = string.Format(RequiredValues, "D", Consts.FixGW_SenderCompID, Consts.FixGW_TargetCompID, strSequence, strDateTimeUTC);
                string strNewOrderFormated = string.Format("1={0}11={1}21={2}38={3}40={4}47={5}54={6}55={7}59={8}60={9}100={10}553={11}", Consts.Account_No, strSequence + strDateTimeUTC, 1, nQuantity, 1, 'I', 2, strSymbol, 1, strDateTimeUTC, "MBTX", Consts.Account_UserName);
                string strBodyStringFormated = string.Format("{0}{1}", strRequiredValuesFormated, strNewOrderFormated);
                string strMessageFormated = string.Format(Message, strBodyStringFormated.Length, strBodyStringFormated);
                strMessageFormated = FixGatewayUtils.CalculateCheckSum(strMessageFormated);

                try
                {
                    // Send request
                    byte[] loginStream = System.Text.Encoding.ASCII.GetBytes(strMessageFormated);
                    networkStream.Write(loginStream, 0, loginStream.Length);
                    Loger.Messages(strMessageFormated);
                    networkStream.Flush();
                }
                catch (Exception e)
                {
                    Loger.ErrorLog(e);
                    return (false);
                }
                finally { FixGatewayUtils.AddToHistoryLog(strSequence, strMessageFormated); }

                return (true);
            }

            return (false);
        }
        public static bool CancelOrder(string strSymbol, string strCIOrdID, bool bIsBuyOrder)
        {
            if (FixGatewayUtils.LogonIndicator)
            {
                int nSide = (bIsBuyOrder ? 1 : 2);
                string strSequence = FixGatewayUtils.GetSecureSequence().ToString();
                string strDateTimeUTC = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss");
                string strRequiredValuesFormated = string.Format(RequiredValues, "F", Consts.FixGW_SenderCompID, Consts.FixGW_TargetCompID, strSequence, strDateTimeUTC);
                string strNewOrderFormated = string.Format("1={0}11={1}41={2}54={3}55={4}59=160={5}553={6}", Consts.Account_No, strSequence + strDateTimeUTC, strCIOrdID, nSide, strSymbol, strDateTimeUTC, Consts.Account_UserName);
                string strBodyStringFormated = string.Format("{0}{1}", strRequiredValuesFormated, strNewOrderFormated);
                string strMessageFormated = string.Format(Message, strBodyStringFormated.Length, strBodyStringFormated);
                strMessageFormated = FixGatewayUtils.CalculateCheckSum(strMessageFormated);

                try
                {
                    // Send request
                    byte[] loginStream = System.Text.Encoding.ASCII.GetBytes(strMessageFormated);
                    networkStream.Write(loginStream, 0, loginStream.Length);
                    Loger.Messages(strMessageFormated);
                    networkStream.Flush();
                }
                catch (Exception e)
                {
                    Loger.ErrorLog(e);
                    return (false);
                }
                finally { FixGatewayUtils.AddToHistoryLog(strSequence, strMessageFormated); }
                return (true);
            }
            
            return (false);
        }
        public static bool UpdateStopLossOrder(string strSymbol, string strCIOrdID, double dStop, int nQuantity)
        {
            if (FixGatewayUtils.LogonIndicator)
            {
                string strSequence = FixGatewayUtils.GetSecureSequence().ToString();
                string strDateTimeUTC = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss");
                string strRequiredValuesFormated = string.Format(RequiredValues, "G", Consts.FixGW_SenderCompID, Consts.FixGW_TargetCompID, strSequence, strDateTimeUTC);
                string strRequestFormated = string.Format("1={0}11={1}21=138={2}99={3}40=341={4}54=255={5}59={6}60={7}553={8}", Consts.Account_No, strSequence + strDateTimeUTC, nQuantity, dStop, strCIOrdID, strSymbol, 1, strDateTimeUTC, Consts.Account_UserName);
                string strBodyStringFormated = string.Format("{0}{1}", strRequiredValuesFormated, strRequestFormated);
                string strMessageFormated = string.Format(Message, strBodyStringFormated.Length, strBodyStringFormated);
                strMessageFormated = FixGatewayUtils.CalculateCheckSum(strMessageFormated);

                try
                {
                    // Send request
                    byte[] loginStream = System.Text.Encoding.ASCII.GetBytes(strMessageFormated);
                    networkStream.Write(loginStream, 0, loginStream.Length);
                    Loger.ExecutionReport(strSymbol, null, false, string.Format("UpdateStopLossOrder - Cid:{0}", strCIOrdID));
                    Loger.Messages(strMessageFormated);
                    networkStream.Flush();
                }
                catch (Exception e)
                {
                    Loger.ExecutionReport(strSymbol, null, false, "UpdateStopLossOrder - Exception");
                    Loger.ErrorLog(e);
                    return (false);
                }
            
                finally { FixGatewayUtils.AddToHistoryLog(strSequence, strMessageFormated); }
                return (true);
            }

            Loger.ExecutionReport(strSymbol, null, false, "UpdateStopLossOrder - LogonIndicator == False");
            return (false);
        }
        public static bool NewStopLossOrder(string strSymbol, double dStop, int nQuantity, out string strClientStopLossOrdID)
        {
            if (FixGatewayUtils.LogonIndicator)
            {
                string strSequence = FixGatewayUtils.GetSecureSequence().ToString();
                string strDateTimeUTC = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss");
                string strClientOrdID = string.Format("{0}{1}", strSequence, strDateTimeUTC);
                strClientStopLossOrdID = strClientOrdID;
                string strRequiredValuesFormated = string.Format(RequiredValues, "D", Consts.FixGW_SenderCompID, Consts.FixGW_TargetCompID, strSequence, strDateTimeUTC);
                string strNewOrderFormated = string.Format("1={0}11={1}21={2}38={3}40={4}47={5}54={6}55={7}59={8}60={9}99={10}100={11}553={12}", Consts.Account_No, strClientOrdID, 1, nQuantity, 3, 'I', 2, strSymbol, 1, strDateTimeUTC, dStop, "MBTX", Consts.Account_UserName);
                string strBodyStringFormated = string.Format("{0}{1}", strRequiredValuesFormated, strNewOrderFormated);
                string strMessageFormated = string.Format(Message, strBodyStringFormated.Length, strBodyStringFormated);
                strMessageFormated = FixGatewayUtils.CalculateCheckSum(strMessageFormated);
                
                try
                {
                    // Send request
                    byte[] loginStream = System.Text.Encoding.ASCII.GetBytes(strMessageFormated);
                    networkStream.Write(loginStream, 0, loginStream.Length);
                    Loger.ExecutionReport(strSymbol, null, false, string.Format("NewStopLossOrder - Cid:{0}", strClientOrdID));
                    Loger.Messages(strMessageFormated);
                    networkStream.Flush();
                }
                catch (Exception e)
                {
                    Loger.ExecutionReport(strSymbol, null, false, "NewStopLossOrder - Exception");
                    Loger.ErrorLog(e);
                    return (false);
                }
                finally { FixGatewayUtils.AddToHistoryLog(strSequence, strMessageFormated); }

                OrdersBook.AddNewClientStopLossOrder(strClientOrdID, strSymbol, nQuantity, string.Empty);
                return (true);
            }

            Loger.ExecutionReport(strSymbol, null, false, "NewStopLossOrder - LogonIndicator == False");
            strClientStopLossOrdID = string.Empty;
            return (false);
        }
        public static bool RequestForPositions()
        {
            if (FixGatewayUtils.LogonIndicator)
            {
                string strSequence = FixGatewayUtils.GetSecureSequence().ToString();
                string strDateTimeUTC = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss");
                string strRequiredValuesFormated = string.Format(RequiredValues, "AN", Consts.FixGW_SenderCompID, Consts.FixGW_TargetCompID, strSequence, strDateTimeUTC);
                string strNewOrderFormated = string.Format("1={0}710={1}724={2}", Consts.Account_No, strSequence + strDateTimeUTC, 0);
                string strBodyStringFormated = string.Format("{0}{1}", strRequiredValuesFormated, strNewOrderFormated);
                string strMessageFormated = string.Format(Message, strBodyStringFormated.Length, strBodyStringFormated);
                strMessageFormated = FixGatewayUtils.CalculateCheckSum(strMessageFormated);

                try
                {
                    // Send request
                    byte[] loginStream = System.Text.Encoding.ASCII.GetBytes(strMessageFormated);
                    networkStream.Write(loginStream, 0, loginStream.Length);
                    Loger.Messages(strMessageFormated);
                    networkStream.Flush();
                    return (true);
                }
                catch (Exception e)
                {
                    Loger.ErrorLog(e);
                    return (false);
                }
                finally { FixGatewayUtils.AddToHistoryLog(strSequence, strMessageFormated); }
            }

            return (false);
        }
        public static void ResendMessages(string strBeginSeqNo)
        {
            try
            {
                string strHistory = FixGatewayUtils.ResendFromHistoryLog(strBeginSeqNo);
                if (strHistory == string.Empty)
                {
                    FixGatewayUtils.SequenceReset(false, 0, 0);
                }
                else
                {
                    // Send request
                    byte[] loginStream = System.Text.Encoding.ASCII.GetBytes(strHistory);
                    networkStream.Write(loginStream, 0, loginStream.Length);
                    Loger.Messages(strHistory);
                    networkStream.Flush();
                }
            }
            catch (Exception e)
            {
                Loger.ErrorLog(e);
                Loger.ErrorLog(new Exception("Couldn't Resend Messages"));
            }
        }


        public static int GetSecureSequence()
        {
            lock (FixGatewayUtils.Sequence)
            {
                FixGatewayUtils.Sequence++;
                return (FixGatewayUtils.Sequence);
            }
        }
        public static string CalculateCheckSum(string strMessage)
        {
            byte bBytesSum = 0;
            for (int i = 0; i < strMessage.Length; i++)
            {
                bBytesSum += (byte)strMessage[i];
            }


            string strSum = "10=";
            for (int m = 3; m > bBytesSum.ToString().Length; m--)
            {
                strSum += '0';
            }
            strSum += bBytesSum.ToString();
            return (string.Format("{0}{1}", strMessage, strSum));
        }

        public static byte[] ConCut(byte[] arr, int nEOA)
        {
            int nArrayLength = arr.Length + 10000;
            byte[] arrToReturn = new byte[nArrayLength];
            for (int nIndex = 0; nIndex < nEOA; nIndex++) { arrToReturn[nIndex] = arr[nIndex]; }
            return (arrToReturn);
        }
        public static void ListenerLoop()
        {
            List<Dictionary<int, string>> lstResponse = null;
            byte[] arrIncomingStream = null;
            int nNumberOfBytesRead;
            int nLastIndex;

            // While program is alive
            while (Program.IsProgramAlive)
            {
                try
                {
                    arrIncomingStream = new byte[10000];
                    nNumberOfBytesRead  = 0;
                    nLastIndex          = 0;
                    
                    do
                    {
                        // Try to read              
                        nNumberOfBytesRead = networkStream.Read(arrIncomingStream, nLastIndex, arrIncomingStream.Length - nLastIndex);

                        if (networkStream.DataAvailable)
                        {
                            nLastIndex += nNumberOfBytesRead;
                            if (nLastIndex == arrIncomingStream.Length) { arrIncomingStream = FixGatewayUtils.ConCut(arrIncomingStream, nLastIndex); } 
                        }
                    }
                    while (networkStream.DataAvailable);

                    lstResponse = FixGatewayUtils.ParseMBTradingMessage(arrIncomingStream);
                }
                catch (Exception e)
                {
                    lstResponse = null;

                    if (Time.IsWeekendNow())
                    {
                        Thread.Sleep(60000);
                    }
                    else if (networkStream != null)
                    {
                        Loger.ErrorLog(e);
                        FixGatewayUtils.CheckIfConnectionError(e);
                    }
                }

                // If there's a response
                if (lstResponse != null)
                {
                    // Run over messages
                    foreach (Dictionary<int, string> dicCurrDic in lstResponse)
                    {
                        Loger.Messages(FixGatewayUtils.UnhandledMessagesToString(dicCurrDic));
                        if ((dicCurrDic.ContainsKey(34)) && (dicCurrDic.ContainsKey(35)))
                            Loger.Sequence((LastGatewaySequence + 1) + " " + dicCurrDic[34] + "          " + dicCurrDic[35]);

                        // If the message correct with the sequence or is one of the spatial messages: Logon \ ResendRequest \ SequenceReset \ TestRequest
                        if ((dicCurrDic.ContainsKey(34)) && (dicCurrDic.ContainsKey(35)) &&
                            ((FixGatewayUtils.LastGatewaySequence + 1 == int.Parse(dicCurrDic[34])) ||
                            (dicCurrDic[35].Equals("A")) ||
                            (dicCurrDic[35].Equals("h")) ||
                            (dicCurrDic[35].Equals("2")) ||
                            (dicCurrDic[35].Equals("4")) ||
                            (dicCurrDic[35].Equals("1"))))
                        {
                            try
                            {
                                switch (dicCurrDic[35])
                                {
                                    #region 0 - Heartbeat
                                    case "0":
                                        {
                                            FixGatewayUtils.LastHB = DateTime.Now;
                                            break;
                                        }
                                    #endregion
                                    #region 1 - Test Request
                                    case "1":
                                        {
                                            FixGatewayUtils.SendHeartbeat(dicCurrDic[112]);
                                            if (FixGatewayUtils.LastGatewaySequence + 1 != int.Parse(dicCurrDic[34])) { FixGatewayUtils.LastGatewaySequence--; }
                                            break;
                                        }
                                    #endregion
                                    #region 2 - Resend Request
                                    case "2":
                                        {
                                            FixGatewayUtils.ResendMessages(dicCurrDic[7]);
                                            break;
                                        }
                                    #endregion
                                    #region 3 - Reject
                                    case "3":
                                        {
                                            break;
                                        }
                                    #endregion
                                    #region 4 - Sequence Reset
                                    case "4":
                                    {
                                        if ((dicCurrDic.ContainsKey(123)) && (dicCurrDic[123] == "Y") && (FixGatewayUtils.LastGatewaySequence + 1 < int.Parse(dicCurrDic[34])))
                                        {
                                            if (FixGatewayUtils.LogonIndicator)
                                            {
                                                FixGatewayUtils.ResendRequest(FixGatewayUtils.LastGatewaySequence + 1, 0);
                                            }
                                        }
                                        else
                                        {
                                            //if (FixGatewayUtils.LastGatewaySequence < int.Parse(dicCurrDic[36]) - 1)
                                            {
                                                FixGatewayUtils.LastGatewaySequence = int.Parse(dicCurrDic[36]) - 1;
                                            }
                                        }
                                        break;
                                    }
                                    #endregion
                                    #region 5 - Logout
                                    case "5":
                                    {
                                        if ((dicCurrDic.ContainsKey(58)) && (dicCurrDic[58].Contains("EOD mandatory")))
                                        {
                                            FixGatewayUtils.LogonIndicator = false;
                                            FixGatewayUtils.OperationalTime = false;
                                            FixGatewayUtils.SystemProblem = true;
                                        }
                                        break;
                                    }
                                    #endregion
                                    #region 6 - Indication of Interest
                                    case "6":
                                        {
                                            break;
                                        }
                                    #endregion
                                    #region 7 - Advertisement
                                    case "7":
                                        {
                                            break;
                                        }
                                    #endregion
                                    #region 8 - Execution Report
                                    case "8":
                                        {
                                            // 14   = Total Quantity filled
                                            // 38   = Ordered Quantity 
                                            // 151  = Leaves Quantity 
                                            // 6    = Average price 
                                            // 99   = Stop
                                            // 37   = OrderID Uniqe
                                            // 11   = CIOrdID 
                                            // 150  = ExecType 
                                            //        A: Pending New
                                            //        0: New
                                            //        F: Trade
                                            // 39   = OrdStatus
                                            //        A: Pending New
                                            //        0: New
                                            //        2: Filled
                                            // 55   = Symbol
                                            // 54   = Side
                                            //        1: Buy
                                            //        2: Sell
                                            // 40   = OrderType
                                            //        1: Market
                                            //        2: Limit
                                            //        3: Stop

                                            Share sCurrShare = Program.SharesList[dicCurrDic[55]];

                                            // Buy Report
                                            if (dicCurrDic[54].Equals("1"))
                                            {
                                                Order oCurrBuyOrder = null;

                                                // Order Canceld Report
                                                if (dicCurrDic[150].Equals("4"))
                                                {
                                                    OrdersBook.OpenOrders.TryGetValue(dicCurrDic[41], out oCurrBuyOrder);

                                                    if (oCurrBuyOrder != null)
                                                    {
                                                        Loger.ExecutionReport(sCurrShare.Symbol, null, true, string.Format("Cid:{0} Oid:{1} CANCELD", oCurrBuyOrder.ClientOrdID, oCurrBuyOrder.OrderID));
                                                        OrdersBook.RemoveBuyOrder(dicCurrDic[41]);

                                                        if (sCurrShare.PositionQuantity == 0)
                                                        {
                                                            sCurrShare.InitializeShere();
                                                        }
                                                    }
                                                }
                                                // If not rejected 
                                                else if (dicCurrDic[150] != "8")
                                                {
                                                    OrdersBook.OpenOrders.TryGetValue(dicCurrDic[11], out oCurrBuyOrder);

                                                    if (oCurrBuyOrder != null)
                                                    {
                                                        bool bHasOrderFilledQuantitoIncreased = oCurrBuyOrder.FGWResponseBuyOrder(dicCurrDic[37], 
                                                                                                                                  double.Parse(dicCurrDic[6]), 
                                                                                                                                  dicCurrDic.ContainsKey(44) ? double.Parse(dicCurrDic[44]) : oCurrBuyOrder.LimitPrice, 
                                                                                                                                  int.Parse(dicCurrDic[14]));

                                                        if (bHasOrderFilledQuantitoIncreased)
                                                        {
                                                            Loger.ExecutionReport(sCurrShare.Symbol, null, true, "Buy Fully placed");
                                                            sCurrShare.Commission += double.Parse(dicCurrDic[12]); // FixGatewayUtils.CalculateCommission(sCurrShare.CandlesList.LastPrice, sCurrShare.Symbol, sCurrShare.PositionQuantity);
                                                        }
                                                    }
                                                }
                                                else if (dicCurrDic[150] == "8")
                                                {
                                                    sCurrShare.InitializeShere();
                                                }
                                            }
                                            // If Sell Report
                                            else if (dicCurrDic[54].Equals("2"))
                                            {
                                                // Order Canceld Report
                                                if (dicCurrDic[150].Equals("4"))
                                                {
                                                    OrdersBook.RemoveStopLossOrder(dicCurrDic[37]);
                                                }
                                                // If was a trade
                                                else if (dicCurrDic[150].Equals("F"))
                                                {
                                                    Order oCurrStopLossOrder = null;

                                                    // if (sCurrShare.StopLossOrders.Count > 0)
                                                    if (OrdersBook.OpenOrders.TryGetValue(dicCurrDic[37], out oCurrStopLossOrder))
                                                    {
                                                        int nOrderQuantityFilled = int.Parse(dicCurrDic[14]);
                                                        double dSoldPrice = double.Parse(dicCurrDic[6]);

                                                        sCurrShare.Commission += double.Parse(dicCurrDic[12]);  //FixGatewayUtils.CalculateCommission(sCurrShare.CandlesList.LastPrice, sCurrShare.Symbol, nOrderQuantityFilled);
                                                        double dProfitOrLoss = FixGatewayUtils.CalculateProfit(sCurrShare.BuyPrice, dSoldPrice, sCurrShare.Symbol, nOrderQuantityFilled);
                                                        MongoDBUtils.DBEventAfterPositionSell(Program.AccountBallance, sCurrShare.Symbol, dProfitOrLoss, sCurrShare.CandleIndex, sCurrShare.CandleIndex - sCurrShare.BuyIndex, sCurrShare.BuyPrice, dSoldPrice);

                                                        sCurrShare.TotalPL += dProfitOrLoss;
                                                        if (dProfitOrLoss < 0)
                                                        { sCurrShare.TotalLoss += dProfitOrLoss; }
                                                        else
                                                        { sCurrShare.TotalProfit += dProfitOrLoss; }
                                                        try { Mail.SendMBTreadingMail(string.Format("{0} P&L is: {1}  (After {2} Change)", sCurrShare.Symbol, Math.Round((sCurrShare.TotalPL - sCurrShare.Commission), 2), dProfitOrLoss.ToString("0.00"))); }
                                                        catch { }
                                                        sCurrShare.PositionQuantity -= nOrderQuantityFilled;

                                                        // For PartialOrders -> The order qountity was filed but there is still position quantity
                                                        if ((int)double.Parse(dicCurrDic[151]) == 0)
                                                        {
                                                            OrdersBook.RemoveStopLossOrder(oCurrStopLossOrder.OrderID);
                                                        }

                                                        // There is no Leaves Quantity 
                                                        if (sCurrShare.PositionQuantity == 0)
                                                        {
                                                            Loger.ExecutionReport(sCurrShare.Symbol, null, false, string.Format("SOLD - SoldPrice: PL:{0}Pips (Diff: LastPrice - SoldPAVG = {1}Pips)", 
                                                                                                                                sCurrShare.PipsUnit * (Math.Round((dSoldPrice - sCurrShare.BuyPrice), 7)),
                                                                                                                                sCurrShare.PipsUnit * (Math.Round((sCurrShare.CandlesList.CurrPrice - dSoldPrice), 7))));
                                                            sCurrShare.SellIndex = sCurrShare.CandleIndex;
                                                            sCurrShare.SellPrice = dSoldPrice;
                                                            sCurrShare.InitializeShere();
                                                        }
                                                        else
                                                        {
                                                            Loger.ExecutionReport(sCurrShare.Symbol, null, false, string.Format("SOLD Partial - SoldPrice: PL:{0}Pips (Diff: LastPrice - SoldPAVG = {1}Pips)",
                                                                                                                                sCurrShare.PipsUnit * (Math.Round((dSoldPrice - sCurrShare.BuyPrice), 7)),
                                                                                                                                sCurrShare.PipsUnit * (Math.Round((sCurrShare.CandlesList.CurrPrice - dSoldPrice), 7))));
                                                        }
                                                    }
                                                }
                                                // If not rejected
                                                else if ((dicCurrDic[150] != "8") && (dicCurrDic[150] != "6"))
                                                {
                                                    Order oStopLossOrder = null;

                                                    // If its a known order
                                                    if (OrdersBook.OpenOrders.ContainsKey(dicCurrDic[37]))
                                                    {
                                                        oStopLossOrder = OrdersBook.OpenOrders[dicCurrDic[37]];
                                                    }
                                                    // If its the first response on a client order
                                                    else if ((dicCurrDic.ContainsKey(11)) && (OrdersBook.OpenOrders.ContainsKey(dicCurrDic[11])))
                                                    {
                                                        OrdersBook.OpenOrders.TryRemove(dicCurrDic[11], out oStopLossOrder);
                                                        OrdersBook.OpenOrders.TryAdd(dicCurrDic[37], oStopLossOrder);
                                                        oStopLossOrder.ParrentShare.StopLossOrders.TryRemove(dicCurrDic[11], out oStopLossOrder);
                                                        oStopLossOrder.ParrentShare.StopLossOrders.TryAdd(dicCurrDic[37], oStopLossOrder);
                                                    }
                                                    // If its the first response on a server order
                                                    else
                                                    {
                                                        Loger.ExecutionReport(sCurrShare.Symbol, null, false, string.Format("Oid:{0} New Server StopLoss", dicCurrDic[37]));
                                                        OrdersBook.AddNewServerStopLossOrder(dicCurrDic[37], dicCurrDic[11], sCurrShare.Symbol, dicCurrDic.ContainsKey(38) ? int.Parse(dicCurrDic[38]) : -1);
                                                        oStopLossOrder = OrdersBook.OpenOrders[dicCurrDic[37]];
                                                    }

                                                    // Update order
                                                    oStopLossOrder.FGWResponseStopLossOrder(dicCurrDic[37],
                                                                                            dicCurrDic[150].Equals("D"), dicCurrDic.ContainsKey(99) ? double.Parse(dicCurrDic[99]) : -1,
                                                                                            dicCurrDic.ContainsKey(38) ? int.Parse(dicCurrDic[38]) : oStopLossOrder.OrderWantedQuantity);
                                                }
                                                else
                                                {
                                                    Order oCurrStopLossOrder = null;
                                                    OrdersBook.OpenOrders.TryGetValue(dicCurrDic[37], out oCurrStopLossOrder);
                                                    oCurrStopLossOrder.FGWResponse = OrderFGWResponse.Rejected;
                                                    Loger.ExecutionReport(sCurrShare.Symbol, null, false, string.Format("Oid:{0} Order Rejected", dicCurrDic[37]));
                                                    // TODO: מה לעשות עם פקודת המכירה או הסטופ נדחת?
                                                }
                                            }

                                            break;
                                        }
                                    #endregion
                                    #region 9 - Order cancel reject
                                    case "9":
                                        {
                                            // If OrdStatus = Rejected
                                            if (dicCurrDic[39].Equals("8"))
                                            {
                                                Order oRejectedOrder = OrdersBook.OpenOrders[dicCurrDic[41]];
                                                if (oRejectedOrder == null)
                                                {
                                                    foreach (Share sCurrShare in Program.SharesList.Values)
	                                                {
	                                                    if ((sCurrShare.BuyOrder != null) && (sCurrShare.BuyOrder.ClientOrdID == dicCurrDic[41]))
                                                        {
                                                            oRejectedOrder = sCurrShare.BuyOrder;
                                                        }
	                                                }
                                                }
                                                Share sOrderParentShare = oRejectedOrder.ParrentShare;
                                                OrdersBook.RemoveBuyOrder(oRejectedOrder.ClientOrdID);
                                                if (!sOrderParentShare.IsPosition)
                                                {
                                                    sOrderParentShare.InitializeShere();
                                                }
                                            }

                                            break;
                                        }
                                    #endregion
                                    #region A - Logon
                                    case "A":
                                        {
                                            if ((int.Parse(dicCurrDic[34]) == 1) || ((dicCurrDic.ContainsKey(141)) && (dicCurrDic[141] == "Y")))
                                            {
                                                FixGatewayUtils.LastGatewaySequence = 1;
                                            }

                                            break;
                                        }
                                    #endregion
                                    #region h - Trading Session Status
                                    case "h":
                                        {
                                            if ((dicCurrDic.ContainsKey(340)) && (dicCurrDic[340] == "2"))
                                            {
                                                FixGatewayUtils.LogonIndicator = true;
                                                FixGatewayUtils.OperationalTime = true;
                                                FixGatewayUtils.SystemProblem = false;
                                                Mail.SendMBTreadingMail("Logon");

                                                if (FixGatewayUtils.LastGatewaySequence + 1 != int.Parse(dicCurrDic[34])) { FixGatewayUtils.LastGatewaySequence--; }
                                            }

                                            break;
                                        }
                                    #endregion
                                    #region AP - Positions Report
                                    case "AP":
                                        {
                                            string strSymbol = dicCurrDic[55];
                                            bool bIsPossition = dicCurrDic.ContainsKey(704) ? int.Parse(dicCurrDic[704]) != 0 : false;
                                            bool bIsPending = dicCurrDic.ContainsKey(10000) ? int.Parse(dicCurrDic[10000]) != 0 : false;
                                            Program.SharesList[strSymbol].PositionsReport = ((bIsPossition) || (bIsPending));
                                            break;
                                        }
                                    #endregion
                                    #region AO - Positions Report ACK
                                    case "AO":
                                        {
                                            break;
                                        }
                                    #endregion
                                    #region default
                                    default:
                                        {
                                            break;
                                        }
                                    #endregion
                                }

                                Loger.PSequence((LastGatewaySequence + 1) + " " + dicCurrDic[34] + "          " + dicCurrDic[35]);
                            }
                            catch (Exception e)
                            {
                                Loger.ErrorLog(e);
                            }
                            finally
                            {
                                FixGatewayUtils.LastHB = DateTime.Now;
                                if (!((dicCurrDic[35].Equals("A")) || 
                                      (dicCurrDic[35].Equals("2")) || 
                                      (dicCurrDic[35].Equals("4"))))
                                {
                                    FixGatewayUtils.LastGatewaySequence++;
                                }
                            }
                        }
                        // If the message is higher than the sequence - preform a resend Request
                        else if ((dicCurrDic.ContainsKey(34)) && (FixGatewayUtils.LastGatewaySequence + 1 < int.Parse(dicCurrDic[34])))
                        {
                            if (FixGatewayUtils.LogonIndicator)
                            {
                                FixGatewayUtils.ResendRequest(FixGatewayUtils.LastGatewaySequence + 1, 0);
                            }
                        }
                    }
                }

                Thread.Sleep(100);
            }
        }
        public static void HeartBeatLoop()
        {
            TimeSpan dtHBDelta = new TimeSpan(0, 1, 30);
            FixGatewayUtils.LastHB = DateTime.Now;
            while (Program.IsProgramAlive)
            {
                if (FixGatewayUtils.OperationalTime)
                {
                    FixGatewayUtils.SendHeartbeat(null);
                    if (DateTime.Now.Subtract(FixGatewayUtils.LastHB) > dtHBDelta)
                    {
                        FixGatewayUtils.CheckIfConnectionError(new Exception("Didn't received Gateway HB"));
                    }
                }

                Thread.Sleep(25000);
            }
        }


        public static void AddToHistoryLog(string strSequence, string strMSG)
        {
            string strDump;
            int nCurrSequence = int.Parse(strSequence);
            if (FixGatewayUtils.MessageHistoryList.ContainsKey(nCurrSequence))
            {
                FixGatewayUtils.MessageHistoryList.TryRemove(nCurrSequence, out strDump);
            }

            while (!FixGatewayUtils.MessageHistoryList.TryAdd(nCurrSequence, strMSG)) { Thread.Sleep(100); Loger.GenericLogFile("UserException","Shit shit shit..."); }
            FixGatewayUtils.MessageHistoryList.TryRemove((nCurrSequence - 5000), out strDump);
        }
        public static string ResendFromHistoryLog(string strBeginSeqNo)
        {
            StringBuilder sbBuilder = new StringBuilder();

            for (int nSequenceIndex = int.Parse(strBeginSeqNo); nSequenceIndex <= FixGatewayUtils.Sequence; nSequenceIndex++)
            {
                if (MessageHistoryList.ContainsKey(nSequenceIndex))
                {
                    if ((MessageHistoryList[nSequenceIndex].Contains("35=D")) ||
                        (MessageHistoryList[nSequenceIndex].Contains("35=2")) ||
                        (MessageHistoryList[nSequenceIndex].Contains("35=A")))
                    {
                        FixGatewayUtils.SequenceReset(true, nSequenceIndex, nSequenceIndex + 1);
                    }

                    sbBuilder.Append(FixGatewayUtils.MessageHistoryList[nSequenceIndex]);
                }
                else
                {
                    return (string.Empty); 
                }
            }

            return (sbBuilder.ToString());
        }
        public static string UnhandledMessagesToString(Dictionary<int, string> dicCurrDic)
        {
            StringBuilder sb = new StringBuilder();
            foreach (int nCurrKey in dicCurrDic.Keys)
            {
                sb.Append(nCurrKey);
                sb.Append("=");
                sb.Append(dicCurrDic[nCurrKey]);
                sb.Append(" ");
            }
            return (sb.ToString());
        }
        public static List<Dictionary<int, string>> ParseMBTradingMessage(byte[] arrMessage)
        {
            Dictionary<int, string> dic = null;
            List<Dictionary<int, string>> lstToRet = new List<Dictionary<int, string>>();

            if (arrMessage != null)
            {
                try
                {
                    int nIndex = 0;
                    int nKey = 0;
                    StringBuilder strValue = new StringBuilder();

                    // Run untill the first message start, in order to find the first full message (8=Fix...)
                    while ((nIndex < arrMessage.Length - 1) && (arrMessage[nIndex] != 0))
                    {
                        if ((arrMessage[nIndex] != 56) && (arrMessage[nIndex + 1] != 61))
                        {
                            nIndex++;
                        }
                        else
                        {
                            if ((nIndex == 0) || (arrMessage[nIndex - 1] == 0x01))
                            {
                                break;
                            }

                            nIndex++;
                        }
                    }


                    dic = new Dictionary<int, string>();

                    // Parse the messages into list of dictionaries
                    while ((nIndex < arrMessage.Length) && (arrMessage[nIndex] != 0))
                    {
                        nKey = 0;
                        strValue.Clear();

                        /////////////// Running for the key /////////////////

                        nKey += arrMessage[nIndex] - 48;
                        nIndex++;
                        // While (arrMessage[nIndex] != '=')
                        while ((arrMessage[nIndex] != 61))                                             // && (arrMessage[nIndex] != 0))
                        {
                            nKey = nKey * 10 + arrMessage[nIndex] - 48;
                            nIndex++;
                        }
                        nIndex++;
                        /////////////////////////////////////////////////////



                        ////////////// Running for the value ////////////////
                        strValue.Append((char)arrMessage[nIndex]);
                        nIndex++;
                        // While (arrMessage[nIndex] != 'SOH')
                        while ((arrMessage[nIndex] != 0x01))                                          // && (arrMessage[nIndex] != 0))
                        {
                            strValue.Append((char)arrMessage[nIndex]);
                            nIndex++;
                        }
                        nIndex++;
                        /////////////////////////////////////////////////////


                        dic.Add(nKey, strValue.ToString());

                        if (nKey == 10)
                        {
                            lstToRet.Add(dic);
                            dic = new Dictionary<int, string>();
                        }
                    }
                }
                catch (Exception e)
                {
                    Loger.ErrorLog(e);
                }
            }
            

            return (lstToRet);
        }


        public static double CalculateProfit(double dOpen, double dClose, string strSymbol, int nQuantity)
        {
            if (dOpen == 0)
            {
                return (0);
            }
            else
            {
                strSymbol = strSymbol.Remove(0, 4);
                double dQuoteToHomeCurrency = 1;

                if (strSymbol != "USD")
                {
                    foreach (string strCurrSymbol in Program.SymbolsNamesList.Keys)
                    {
                        if (strCurrSymbol.Contains(strSymbol))
                        {
                            if (strCurrSymbol.Equals(strSymbol + "/USD"))
                            {
                                dQuoteToHomeCurrency = Program.SharesList[strCurrSymbol].CandlesList.CurrPrice;

                                break;
                            }
                            else if (strCurrSymbol.Equals("USD/" + strSymbol))
                            {
                                dQuoteToHomeCurrency = 1 / Program.SharesList[strCurrSymbol].CandlesList.CurrPrice;

                                break;
                            }
                            else
                            {
                            }
                        }
                    }
                }
                return ((dClose - dOpen) * dQuoteToHomeCurrency * nQuantity);
            }
        }
        public static double CalculateRateToProfit(double dPL, double dOpen, string strSymbol, int nQuantity)
        {
            strSymbol = strSymbol.Remove(0, 4);
            double dQuoteToHomeCurrency = 1;
            foreach (string strCurrSymbol in Program.SymbolsNamesList.Keys)
            {
                if (strCurrSymbol.Contains(strSymbol))
                {
                    if (strCurrSymbol.Equals(strSymbol + "/USD"))
                    {
                        dQuoteToHomeCurrency = Program.SharesList[strCurrSymbol].CandlesList.CurrPrice;

                        break;
                    }
                    else if (strCurrSymbol.Equals("USD/" + strSymbol))
                    {
                        dQuoteToHomeCurrency = 1 / Program.SharesList[strCurrSymbol].CandlesList.CurrPrice;

                        break;
                    }
                    else
                    {
                    }
                }
            }

            return ((dPL / (dQuoteToHomeCurrency * nQuantity)) + dOpen);
        }
        public static double CalculateCommission(double dRate, string strSymbol, int nQuantity)
        {
            if (strSymbol.Contains("USD/"))
            {
                return (nQuantity * 0.000025);
            }
            else if (strSymbol.Contains("/USD"))
            {
                return (nQuantity * dRate * 0.000025);
            }
            else
            {
                strSymbol = strSymbol.Remove(3, 4);
                foreach (string strCurrSymbol in Program.SymbolsNamesList.Keys)
                {
                    if (strCurrSymbol.Contains(strSymbol))
                    {
                        if (strCurrSymbol.Equals(strSymbol + "/USD"))
                        {
                            return (nQuantity * Program.SharesList[strCurrSymbol].CandlesList.CurrPrice * 0.000025);
                        }
                    }
                }

                return (nQuantity * 0.000025);
            }
        }
    }
}