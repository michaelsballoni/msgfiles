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

        public bool 
            BeginConnect
            (
                string hostname, 
                int port, 
                string displayName, 
                string email, 
                string session
            )
        {
            m_app.Log($"Connecting {hostname} : {port}...");
            m_client = new TcpClient(hostname, port);
            if (m_app.Cancelled)
                return false;

            m_app.Log($"Securing connection...");
            m_stream = SecureNet.SecureConnectionToServer(m_client, hostname);
            if (m_app.Cancelled)
                return false;

            m_app.Log($"Starting authentication...");
            var auth_request =
                new ClientRequest()
                {
                    version = 1,
                    verb = "AUTH",
                    headers =
                        new Dictionary<string, string>()
                        {
                            { "display", displayName },
                            { "email", email },
                            { "session", session }
                        }
                };
            SecureNet.SendObject(m_stream, auth_request);
            if (m_app.Cancelled)
                return false;

            var auth_response = SecureNet.ReadObject<ServerResponse>(m_stream);
            m_app.Log($"Server Response: {auth_response.ResponseSummary}");
            switch (auth_response.statusCode)
            {
                case 200:
                    return false;
                case 401:
                    return true;
                default:
                    throw auth_response.CreateException();
            }
        }

        public void ContinueConnect(string challengeToken)
        {
            m_app.Log("Sending challenge response...");
            var auth_submit =
                new ClientRequest()
                {
                    version = 1,
                    verb = "CHALLENGE",
                    headers = new Dictionary<string, string>() { { "challenge", challengeToken } }
                };
            if (m_stream == null)
                throw new NetworkException("Not connected");
            SecureNet.SendObject(m_stream, auth_submit);

            var auth_response = SecureNet.ReadObject<ServerResponse>(m_stream);
            m_app.Log($"Server Response: {auth_response.ResponseSummary}");
            switch (auth_response.statusCode)
            {
                case 200:
                    SessionToken = auth_response.headers["session"];
                    break;
                default:
                    throw auth_response.CreateException();
            }
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

        private IClientApp m_app;

        public string SessionToken { get; set; } = "";

        private TcpClient? m_client = null;
        private Stream? m_stream = null;
    }
}
