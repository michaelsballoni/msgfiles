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

        /// <summary>
        /// Start a connection with the server
        /// </summary>
        /// <returns>true if server is challenging client, false if ready to process</returns>
        public bool 
            BeginConnect
            (
                string serverHostname, 
                int serverPort, 
                string displayName, 
                string email
            )
        {
            Disconnect();

            m_app.Log($"Connecting {serverHostname} : {serverPort}...");
            m_client = new TcpClient(serverHostname, serverPort);
            m_client.NoDelay = true;
            if (m_app.Cancelled)
                return false;

            m_app.Log($"Securing connection...");
            m_stream = SecureNet.SecureConnectionToServer(m_client, serverHostname);
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
                            { "session", SessionToken }
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
                    SessionToken = auth_response.headers["session"];
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
