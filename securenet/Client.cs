﻿using System.Net.Sockets;

namespace msgfiles
{
    /// <summary>
    /// Base class of MsgClient, handles user authentication
    /// </summary>
    public class Client : IDisposable
    {
        // A little dependency injection
        public Client(IClientApp app)
        {
            App = app;
        }

        public void Dispose()
        {
            Disconnect();
        }

        public void Disconnect()
        {
            try
            {
                if (ServerStream != null)
                {
                    ServerStream.Dispose();
                }
            }
            catch { }
            ServerStream = null;

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
            // Start fresh
            Disconnect();

            // Make TCP connection
            App.Log($"Connecting {serverHostname} : {serverPort}...");
            m_client = new TcpClient(serverHostname, serverPort);
            m_client.NoDelay = true; // requests should be sent out ASAP
            if (App.Cancelled)
                return false;

            // Do the SslStream thing
            App.Log($"Securing connection...");
            ServerStream = SecureNet.SecureConnectionToServer(m_client);
            if (App.Cancelled)
                return false;

            // Send in our AUTH request with the info we have on hand
            App.Log($"Starting authentication...");
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
            SecureNet.SendObject(ServerStream, auth_request);
            if (App.Cancelled)
                return false;

            // Get the response, either session token, challenge required, or fail
            var auth_response = SecureNet.ReadObject<ServerResponse>(ServerStream);
            App.Log($"Server Response: {auth_response.ResponseSummary}");
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

        /// <summary>
        /// Given the challenge token provided by the user
        /// try to continue the authentication
        /// </summary>
        public void ContinueConnect(string challengeToken)
        {
            // Send special CHALLENGE request with the token
            App.Log("Sending challenge response...");
            var auth_submit =
                new ClientRequest()
                {
                    version = 1,
                    verb = "CHALLENGE",
                    headers = new Dictionary<string, string>() { { "challenge", challengeToken } }
                };
            if (ServerStream == null)
                throw new NetworkException("Not connected");
            SecureNet.SendObject(ServerStream, auth_submit);

            // Receive the response, either success with session or fail
            var auth_response = SecureNet.ReadObject<ServerResponse>(ServerStream);
            App.Log($"Server Response: {auth_response.ResponseSummary}");
            switch (auth_response.statusCode)
            {
                case 200:
                    SessionToken = auth_response.headers["session"];
                    break;
                default:
                    throw auth_response.CreateException();
            }
        }


        public IClientApp App;
        public Stream? ServerStream;

        public string SessionToken = "";

        private TcpClient? m_client = null;
    }
}
