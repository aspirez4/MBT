using System;
using System.Net;
using System.Net.Mail;

namespace MBTrading
{
    public static class Mail
    {
        public static void SendMBTreadingMail(string strMessage)
        {
            MailAddress maFromAddress       = new MailAddress("MBTreading@Yahoo.com", "MBTreading");
            MailAddress maToAddress         = new MailAddress("orshapira91@gmail.com", "MBTreading");
            string strFromPassword          = "Aa123456";
            string strSubject               = string.Format("MBTreading - {0}", DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
            string strBody                  = strMessage;

            SmtpClient SMTP = new SmtpClient
            {
                Host                    = "smtp.mail.yahoo.com",
                Port                    = 587,
                EnableSsl               = true,
                DeliveryMethod          = SmtpDeliveryMethod.Network,
                UseDefaultCredentials   = false,
                Credentials             = new NetworkCredential(maFromAddress.Address, strFromPassword)
            };
            using (MailMessage mmMessage = new MailMessage(maFromAddress, maToAddress)
                                                            {
                                                                Subject = strSubject,
                                                                Body    = strBody
                                                            })
            {
                try { SMTP.Send(mmMessage); }
                catch { }
            }
        }
    }
}
