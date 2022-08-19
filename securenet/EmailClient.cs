using System.Net;
using System.Net.Mail;

namespace msgfiles
{
    public class EmailClient
    {
        public EmailClient(string server, int port, string username, string password)
        {
            m_server = server;
            m_port = port;
            m_username = username;
            m_password = password;
        }

        public async Task SendEmailAsync
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
            client.Credentials = new NetworkCredential(m_username, m_password);
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.EnableSsl = true;

            client.SendAsync(mail_message, null);
            await Task.FromResult(0);
        }

        private string m_server;
        private int m_port;
        private string m_username;
        private string m_password;
    }
}
