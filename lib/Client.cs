using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace msgfiles
{
    public class Client : IDisposable
    {
        public Client(ILog log)
        {
            m_log = log;
        }

        public void Dispose()
        {
            DisconnectAsync();
        }

        public async Task BeginConnectAsync(string hostname, int port, string displayName, string email)
        {
            m_stream = await SecureNet.ConnectToSecureServer(hostname, port, m_log).ConfigureAwait(false);

            AuthInfo auth_info = new AuthInfo() { Display = displayName, Email = email, SessionToken = m_session };
            await SecureNet.SendObjectAsync(m_stream, auth_info).ConfigureAwait(false);
        }

        public async Task CompleteConnectAsync()
        {
            AuthSubmit auth_submit = new AuthSubmit() { ChallengeToken = ChallengeToken };
            await SecureNet.SendObjectAsync(m_stream, auth_submit).ConfigureAwait(false);

            AuthResponse auth_response = await SecureNet.ReceiveObjectAsync<AuthResponse>(m_stream).ConfigureAwait(false);
            m_session = auth_response.SessionToken;
        }

        public async Task DisconnectAsync()
        {
            if (m_stream != null)
            {
                m_stream.Dispose();
                m_stream = null;
            }
        }

        public string ChallengeToken { get; set; } = "";

        ILog m_log;
        string m_session = "";
        Stream m_stream = null;
    }
}
