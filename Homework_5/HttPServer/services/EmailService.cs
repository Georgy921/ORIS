using System.Net;
using System.Net.Mail;
using HttPServer.Settings;
using HttPServer.Suka;


namespace HttpServer.Services
{
    public static class EmailService
    {
        public static void SendEmail(string to, string subject, string message)
        {
            MailAddress fromUser = new MailAddress(
              Global.Settings.Model.EmailFrom,
              Global.Settings.Model.EmailNameFrom);
                MailAddress toUser = new MailAddress(to);
                MailMessage m = new MailMessage(fromUser, toUser);

            m.Subject = subject;
            m.Body = $"<h2>{message}</h2>";
            m.IsBodyHtml = true;

            SmtpClient smtp = new SmtpClient(
                Global.Settings.Model.SmtpHost,
                int.Parse(Global.Settings.Model.SmtpPort));

            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential(
                Global.Settings.Model.EmailFrom,
                Global.Settings.Model.SmtpPassword);
            smtp.EnableSsl = true;
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtp.Send(m);
            smtp.Dispose();
        }
    }
}