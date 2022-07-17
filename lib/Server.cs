using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

using Newtonsoft.Json;

namespace msgfiles
{
    public class Server : IDisposable
    {
        public Server(ILog log, ITokenSender tokenSender, IAppOperator appOperator, int port, string hostname)
        {
            m_log = log;
            m_tokenSender = tokenSender;
            m_operator = appOperator;

            m_port = port;
            m_listener = new TcpListener(IPAddress.Loopback, m_port);
            m_cert = SecureNet.GenCert(hostname);
        }

        public void Dispose()
        {
            Stop();
        }

        public void AcceptConnections()
        {
            try
            {
                m_listener.Start();
                Ready = true;
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
            }
        }

        public bool Ready { get; private set; } = false;

        public void Stop()
        {
            if (!m_keepRunning)
                return;

            m_log.Log("Stop: Submitting poison pill");
            m_keepRunning = false;
            try
            {
                TcpClient poison_pill = new TcpClient("127.0.0.1", m_port);
                poison_pill.Dispose();
            }
            catch { }

            m_log.Log("Stop: Waiting on stop");
            while (!m_stopped)
                Thread.Sleep(100);

            m_log.Log("Stop: Closing connections");
            List<TcpClient> clients = new List<TcpClient>(m_clients);
            foreach (var client in clients)
            {
                try
                {
                    client.Close();
                }
                catch { }
            }

            m_log.Log("Stop: Waiting on connections");
            while (true)
            {
                lock (m_clients)
                {
                    if (m_clients.Count == 0)
                        break;
                }
                Thread.Sleep(100);
            }
            m_log.Log("Stop: All done");
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                LogClient(client, "Securing");
                lock (m_clients)
                    m_clients.Add(client);
                using (Stream stream = await SecureNet.SecureRemoteConnectionAsync(client, m_cert).ConfigureAwait(false))
                {
                    LogClient(client, "Auth info");
                    AuthInfo auth_info = await SecureNet.ReceiveObjectAsync<AuthInfo>(stream).ConfigureAwait(false);
                    auth_info.Normalize();

                    Session session = null;
                    if (!string.IsNullOrWhiteSpace(auth_info.SessionToken))
                        session = m_operator.GetSession(auth_info);

                    if (session == null)
                    {
                        LogClient(client, "Token send");
                        string challenge_token = Utils.GenToken();
                        m_tokenSender.SendToken(challenge_token);

                        LogClient(client, "Auth submit");
                        AuthSubmit auth_submit = await SecureNet.ReceiveObjectAsync<AuthSubmit>(stream).ConfigureAwait(false);
                        if (auth_submit.ChallengeToken != challenge_token)
                        {
                            LogClient(client, "Incorrect auth submit");
                            return;
                        }

                        LogClient(client, "Session create");
                        session = m_operator.CreateSession(auth_info);
                    }

                    AuthResponse auth_response = new AuthResponse() { SessionToken = session.Token };
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
            m_log.Log($"{client.Client.RemoteEndPoint}: {message}");
        }

        private ILog m_log;
        private ITokenSender m_tokenSender;
        private IAppOperator m_operator;

        private int m_port;
        private TcpListener m_listener;
        private X509Certificate m_cert;

        private bool m_keepRunning = true;
        private bool m_stopped = false;

        private List<TcpClient> m_clients = new List<TcpClient>();
    }
}
