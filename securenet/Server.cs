using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace msgfiles
{
    /// <summary>
    /// General purpose secure socket server
    /// This class manages listening for connections
    /// and maintaining a registry of connections
    /// ServerClient does the work of handling connections
    /// </summary>
    public class Server : IDisposable
    {
        public Server(IServerApp app, int port)
        {
            m_app = app;
            m_port = port;

            m_cert = SecureNet.GenCert();

            m_listener = new TcpListener(IPAddress.Any, m_port);
        }

        public static int ReceiveTimeoutSeconds;

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
                    new_client.NoDelay = true;
                    if (ReceiveTimeoutSeconds > 0)
                        new_client.ReceiveTimeout = ReceiveTimeoutSeconds * 1000;

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

            m_app.Log("Stop: Submitting poison pill...");
            m_keepRunning = false;
            try
            {
                TcpClient poison_pill = new TcpClient("127.0.0.1", m_port);
                poison_pill.NoDelay = true;
                poison_pill.Dispose();
            }
            catch { }

            m_app.Log("Stop: Waiting on listening to stop...");
            while (!m_stopped)
                Thread.Sleep(100);

            m_app.Log("Stop: Closing connections...");
            List<TcpClient> clients;
            lock (m_clients)
                clients = new List<TcpClient>(m_clients);
            foreach (var client in clients)
            {
                try
                {
                    client.Close();
                }
                catch { }
            }

            m_app.Log("Stop: Waiting on connections...");
            while (true)
            {
                lock (m_clients)
                {
                    if (m_clients.Count == 0)
                        break;
                }
                Thread.Sleep(100);
            }
            m_app.Log("Stop: All done.");
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            lock (m_clients)
                m_clients.Add(client);
            try
            {
                using (var server_client = new ServerClient(m_app, m_cert, client))
                    await server_client.HandleClientAsync().ConfigureAwait(false);
            }
            finally
            {
                try { client.Dispose(); } catch { }
                lock (m_clients)
                    m_clients.Remove(client);
            }
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
