using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace msgfiles
{
    public class Server : IDisposable
    {
        public Server(IServerApp app, int port, string hostname)
        {
            m_app = app;

            m_port = port;
            m_listener = new TcpListener(IPAddress.Loopback, m_port);
            m_cert = SecureNet.GenCert(hostname);
        }

        public void Dispose()
        {
            Stop();
        }

        public void Accept()
        {
            try
            {
                m_listener.Start();
                Ready = true;
                m_app.Log("Listening: Started");
                while (true)
                {
                    TcpClient new_client = m_listener.AcceptTcpClient();
                    if (!m_keepRunning)
                        break;
                    else
                        Task.Run(async () => await HandleClientAsync(new_client).ConfigureAwait(false));
                }
                Ready = false;
                m_listener.Stop();
            }
            finally
            {
                m_stopped = true;
                m_app.Log("Listening: Stopped");
            }
        }

        public bool Ready { get; private set; } = false;

        public void Stop()
        {
            if (!m_keepRunning)
                return;

            m_app.Log("Stop: Submitting poison pill");
            m_keepRunning = false;
            try
            {
                TcpClient poison_pill = new TcpClient("127.0.0.1", m_port);
                poison_pill.Dispose();
            }
            catch { }

            m_app.Log("Stop: Waiting on listening to stop");
            while (!m_stopped)
                Thread.Sleep(100);

            m_app.Log("Stop: Closing connections");
            List<TcpClient> clients = new List<TcpClient>(m_clients);
            foreach (var client in clients)
            {
                try
                {
                    client.Close();
                }
                catch { }
            }

            m_app.Log("Stop: Waiting on connections");
            while (true)
            {
                lock (m_clients)
                {
                    if (m_clients.Count == 0)
                        break;
                }
                Thread.Sleep(100);
            }
            m_app.Log("Stop: All done");
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                LogClient(client, "Securing");
                lock (m_clients)
                    m_clients.Add(client);
                using (Stream stream = await SecureNet.SecureConnectionFromClient(client, m_cert).ConfigureAwait(false))
                {
                    LogClient(client, "Auth info");
                    var auth_info = await SecureNet.ReadHeadersAsync(stream).ConfigureAwait(false);
                    Utils.NormalizeDict(auth_info, new[] { "display", "email", "session" });
                    if (string.IsNullOrEmpty(auth_info["display"]) || string.IsNullOrEmpty(auth_info["email"]))
                        throw new Exception("Auth info missing fields");

                    Session? session = null;
                    if (!string.IsNullOrEmpty(auth_info["session"]))
                        session = m_app.GetSession(auth_info);

                    var auth_init_response =
                        new Dictionary<string, string>()
                        { { "challenge_required", (session == null).ToString()} };
                    await SecureNet.SendObjectAsync(stream, auth_init_response).ConfigureAwait(false);

                    if (session == null)
                    {
                        LogClient(client, "Token send");
                        string challenge_token = Utils.GenToken();
                        m_app.SendChallengeToken(auth_info["email"], challenge_token);

                        LogClient(client, "Auth submit");
                        var auth_submit = await SecureNet.ReadHeadersAsync(stream).ConfigureAwait(false);
                        Utils.NormalizeDict(auth_submit, new[] { "challenge" });
                        if (auth_submit["challenge"] != challenge_token)
                        {
                            LogClient(client, "Incorrect auth submit");
                            return;
                        }

                        LogClient(client, "Session create");
                        session = m_app.CreateSession(auth_info);
                    }

                    var auth_response = new Dictionary<string, string>() { { "session", session.token } };
                    await SecureNet.SendObjectAsync(stream, auth_response).ConfigureAwait(false);

                    // FORNOW - Handle connection operations here
                }
            }
            catch (Exception e)
            {
                LogClient(client, $"Error: {e.GetType().FullName}: {e.Message}");
            }
            finally
            {
                try { client.Dispose(); } catch { }
                lock (m_clients)
                    m_clients.Remove(client);
            }
        }

        private void LogClient(TcpClient client, string message)
        {
            m_app.Log($"{client.Client.RemoteEndPoint}: {message}");
        }

        private IServerApp m_app;

        private int m_port;
        private TcpListener m_listener;
        private X509Certificate m_cert;

        private bool m_keepRunning = true;
        private bool m_stopped = false;

        private List<TcpClient> m_clients = new List<TcpClient>();
    }
}
