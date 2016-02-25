using System;
using System.IO;
using System.Text;

namespace MBTrading
{
    public static class Loger
    {
        public static void ErrorLog(Exception e)
        {
            StringBuilder sbExceptionBuilderException = new StringBuilder();
            StringBuilder sbExceptionBuilderStackTrace = new StringBuilder();
            sbExceptionBuilderException.AppendLine(e.Message);
            sbExceptionBuilderStackTrace.AppendLine(e.StackTrace);
            while (e.InnerException != null)
            {
                e = e.InnerException;
                sbExceptionBuilderException.Append("                    - ");
                sbExceptionBuilderException.AppendLine(e.Message);
                sbExceptionBuilderStackTrace.Append("                    - ");
                sbExceptionBuilderStackTrace.AppendLine(e.StackTrace);
            }
            try
            {
                File.AppendAllText(string.Format("{0}\\ErrorLog.txt", Consts.FilesPath),
                                   string.Format("{0}    - {1}", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), sbExceptionBuilderException.ToString()));
                File.AppendAllText(string.Format("{0}\\ErrorLogStackTrace.txt", Consts.FilesPath),
                                   string.Format("{0}    - {1}", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), sbExceptionBuilderStackTrace.ToString()));
            }
            catch { }
        }
        public static void ExecutionReport(string strSymbol, Order oCurrOrder, bool bBuySide, string strText)
        {
            try
            {
                if (oCurrOrder != null)
                {
                    File.AppendAllText(string.Format("{0}\\Ex_{1}.txt", Consts.FilesPath, strSymbol.Remove(3, 1)),
                                       string.Format("{0}       -         {1} Cid:{2}  Oid:{3} Prc:{4} Qun:{5}\r\n", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), 
                                                                                                                     oCurrOrder.Progress, 
                                                                                                                     oCurrOrder.ClientOrdID, 
                                                                                                                     oCurrOrder.OrderID, 
                                                                                                                     bBuySide ? oCurrOrder.LimitPrice : oCurrOrder.StopLossPrice, 
                                                                                                                     bBuySide ? oCurrOrder.OrderFilledQuantity : oCurrOrder.OrderWantedQuantity));
                }
                else
                {
                    File.AppendAllText(string.Format("{0}\\Ex_{1}.txt", Consts.FilesPath, strSymbol.Remove(3, 1)),
                                       string.Format("{0}       - {1}: {2}\r\n", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                                                                                 bBuySide ? "B" : "S",
                                                                                 strText));
                }

            }
            catch { GenericLogFile("ExecutionsReportCatch", "problem in loging to ExecutionReport log"); }
        }
        public static void Messages(string strString)
        {
            try
            {
                File.AppendAllText(string.Format("{0}\\Messages.txt", Consts.FilesPath),
                                   string.Format("{0}       - {1}\r\n", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), strString));
            }
            catch { }
        }
        public static void Sequence(string nSequence)
        {
            try
            {
                File.AppendAllText(string.Format("{0}\\Sequence.txt", Consts.FilesPath),
                                   string.Format("{0}    - {1}\r\n", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), nSequence));
            }
            catch { }
        }
        public static void PSequence(string nSequence)
        {
            try
            {
                File.AppendAllText(string.Format("{0}\\PSequence.txt", Consts.FilesPath),
                                   string.Format("{0}    - {1}\r\n", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), nSequence));
            }
            catch { }
        }
        public static void GenericLogFile(string strFileName, string strText)
        {
            try
            {
                File.AppendAllText(string.Format("{0}\\{1}.txt", Consts.FilesPath, strFileName),
                                   string.Format("{0}    - {1}\r\n", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), strText));
            }
            catch { }
        }
    }
}
