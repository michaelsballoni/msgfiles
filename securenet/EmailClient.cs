using System.Net;
using System.Net.Mail;

namespace msgfiles
{
    /// <summary>
    /// SmtpClient wrapper class
    /// </summary>
    public class EmailClient
    {
        public EmailClient(string server, int port, string username, string password)
        {
            m_server = server;
            m_port = port;
            m_credential = new NetworkCredential(username, password);
        }

        public void SendEmail
        (
            string from, // display <email> or just email
            Dictionary<string, string> toAddrs, // email -> display
            string subject,
            string body
        )
        {
            var fromKvp = Utils.ParseEmail(from);

            var mail_message = new MailMessage();
            mail_message.From = new MailAddress(fromKvp.Key, fromKvp.Value);

            foreach (var toKvp in toAddrs)
                mail_message.To.Add(new MailAddress(toKvp.Key, toKvp.Value));

            mail_message.Subject = subject;
            mail_message.Body = body;

            SmtpClient client = new SmtpClient(m_server, m_port);
            client.Credentials = m_credential;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.EnableSsl = true;
            client.SendAsync(mail_message, null);
        }

        private string m_server;
        private int m_port;
        private NetworkCredential m_credential;
    }
}
