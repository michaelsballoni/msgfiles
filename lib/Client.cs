using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace msgfiles
{
    public class Client : IDisposable
    {
        public Client(IApp app)
        {
            m_app = app;
        }

        public void Dispose()
        {
            Disconnect();
        }

        public async Task BeginConnectAsync(string hostname, int port, string displayName, string email)
        {
            m_app.Log($"Connecting {hostname} : {port}");
            var client = new TcpClient(hostname, port);

            m_app.Log($"Authenticating {hostname} : {port}");
            m_stream = await SecureNet.ConnectToSecureServer(client).ConfigureAwait(false);

            var auth_info =
                new Dictionary<string, string>()
                {
                    { "display", displayName },
                    { "email", email },
                    { "session", m_session }
                };
            await SecureNet.SendHeadersAsync(m_stream, auth_info).ConfigureAwait(false);
        }

        public async Task CompleteConnectAsync()
        {
            var auth_submit = new Dictionary<string, string>() { { "challenge", ChallengeToken } };
            await SecureNet.SendHeadersAsync(m_stream, auth_submit).ConfigureAwait(false);

            var auth_response = await SecureNet.ReadHeadersAsync(m_stream).ConfigureAwait(false);
            m_session = auth_response["session"];
        }

        public void Disconnect()
        {
            try
            {
                if (m_stream != null)
                {
                    m_stream.Dispose();
                }
            }
            catch { }
            m_stream = null;

            try
            {
                if (m_client != null)
                {
                    m_client.Dispose();
                }
            }
            catch { }
            m_client = null;
        }

        public string ChallengeToken { get; set; } = "";

        IApp m_app;

        string m_session = "";

        TcpClient m_client = null;
        Stream m_stream = null;
    }
}
