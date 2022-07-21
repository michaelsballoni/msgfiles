using System.Net.Sockets;

namespace msgfiles
{
    public class Client : IDisposable
    {
        public Client(IClientApp app)
        {
            m_app = app;
        }

        public void Dispose()
        {
            Disconnect();
        }

        public async Task<bool> BeginConnectAsync(string hostname, int port, string displayName, string email)
        {
            m_app.Log($"Connecting {hostname} : {port}...");
            var client = new TcpClient(hostname, port);

            m_app.Log($"Securing connection...");
            m_stream = await SecureNet.SecureConnectionToServer(client, hostname).ConfigureAwait(false);

            m_app.Log($"Starting authentication...");
            var auth_info =
                new Dictionary<string, string>()
                {
                    { "display", displayName },
                    { "email", email },
                    { "session", m_session }
                };
            await SecureNet.SendObjectAsync(m_stream, auth_info).ConfigureAwait(false);

            var auth_response = await SecureNet.ReadHeadersAsync(m_stream).ConfigureAwait(false);
            Utils.NormalizeDict(auth_response, new[] { "challenge_required" });
            string challenge_required_str = auth_response["challenge_required"];
            bool challenge_required;
            if (!bool.TryParse(challenge_required_str, out challenge_required))
                throw new Exception("Invalid server response");
            else
                return challenge_required;
        }

        public async Task ContinueConnectAsync(string challengeToken)
        {
            var auth_submit = new Dictionary<string, string>() { { "challenge", challengeToken } };
            if (m_stream == null)
                throw new Exception("Not connected");
            await SecureNet.SendObjectAsync(m_stream, auth_submit).ConfigureAwait(false);
        }

        public async Task CompleteConnectAsync()
        {
            if (m_stream == null)
                throw new Exception("Not connected");
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

        IClientApp m_app;

        string m_session = "";

        TcpClient? m_client = null;
        Stream? m_stream = null;
    }
}
