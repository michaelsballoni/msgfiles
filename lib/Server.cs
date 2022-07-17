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
    public class Server : IDisposable, ILog, ITokenSender
    {
        public Server(ILog log, ITokenSender tokenSender, int port, string hostname)
        {
            m_log = log;
            m_tokenSender = tokenSender;

            m_port = port;
            m_listener = new TcpListener(IPAddress.Loopback, m_port);
            m_cert = SecureNet.GenCert(hostname);
        }

        public void Dispose()
        {
            Stop();
        }

        public async Task AcceptConnectionsAsync()
        {
            try
            {
                m_listener.Start();
                while (true)
                {
                    TcpClient new_client = await m_listener.AcceptTcpClientAsync();
                    if (!m_keepRunning)
                        break;
                    else
                        Task.Run(async () => await HandleClientAsync(new_client).ConfigureAwait(false));
                }
                m_listener.Stop();
            }
            finally
            {
                m_stopped = true;
            }
        }

        public void Stop()
        {
            if (!m_keepRunning)
                return;

            Log("Stop: Submitting poison pill");
            m_keepRunning = false;
            try
            {
                TcpClient poison_pill = new TcpClient(new IPEndPoint(IPAddress.Loopback, m_port));
                poison_pill.Dispose();
            }
            catch { }

            Log("Stop: Waiting on stop");
            while (!m_stopped)
                Thread.Sleep(10);

            Log("Stop: Closing connections");
            List<TcpClient> clients = new List<TcpClient>(m_clients);
            foreach (var client in clients)
            {
                try
                {
                    client.Close();
                }
                catch { }
            }

            Log("Stop: Waiting on connections");
            while (true)
            {
                lock (m_clients)
                {
                    if (m_clients.Count == 0)
                        break;
                }
                Thread.Sleep(10);
            }
            Log("Stop: All done");
        }

        public void Log(string message)
        {
            m_log.Log(message);
        }

        public void SendToken(string token)
        {
            m_tokenSender.SendToken(token);
        }

        public void LogClient(TcpClient client, string message)
        {
            Log($"{client.Client.RemoteEndPoint}: {message}");
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
                    AuthInfo auth_info;
                    {
                        string header = await SecureNet.ReadHeaderAsync(stream, 64 * 1024);
                        if (header.Length == 0)
                        {
                            LogClient(client, "Invalid auth info header");
                            return;
                        }
                        auth_info = (AuthInfo)JsonConvert.DeserializeObject<AuthInfo>(header);
                    }
                    if (auth_info == null)
                    {
                        LogClient(client, "Invalid auth info");
                        return;
                    }
                    auth_info.Display = auth_info.Display.Trim();
                    auth_info.Email = auth_info.Email.Trim();
                    if (auth_info.Display.Length == 0 || auth_info.Email.Length == 0)
                    {
                        LogClient(client, "Missing auth info");
                        return;
                    }

                    LogClient(client, "Token send");
                    string sent_token = Utils.Hash256Str(Guid.NewGuid().ToString() + DateTime.UtcNow.Ticks);
                    SendToken(sent_token);

                    LogClient(client, "Auth submit");
                    AuthSubmit auth_submit;
                    {
                        string header = await SecureNet.ReadHeaderAsync(stream, 64 * 1024);
                        if (header.Length == 0)
                        {
                            LogClient(client, "Invalid auth submit header");
                            return;
                        }

                        auth_submit = (AuthSubmit)JsonConvert.DeserializeObject<AuthSubmit>(header);
                    }
                    if (auth_submit == null)
                    {
                        LogClient(client, "Invalid auth submit");
                        return;
                    }
                    auth_submit.Token = auth_submit.Token.Trim();
                    if (auth_submit.Token.Length == 0)
                    {
                        LogClient(client, "Missing auth submit");
                        return;
                    }

                    if (auth_submit.Token != sent_token)
                    {
                        LogClient(client, "Incorrect auth submit");
                        return;
                    }

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

        private ILog m_log;
        private ITokenSender m_tokenSender;

        private int m_port;
        private TcpListener m_listener;
        private X509Certificate m_cert;

        private bool m_keepRunning = true;
        private bool m_stopped = false;

        private List<TcpClient> m_clients = new List<TcpClient>();
    }
}
