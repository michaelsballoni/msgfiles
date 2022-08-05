using System.Net.Sockets;

using Ionic.Zip;

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

        public bool SendMsg(Msg msg)
        {
            string pwd = Utils.GenToken();
            string zip_file_path =
                Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
            try
            {
                using (var zip = new ZipFile(zip_file_path))
                {
                    m_app.Log("Adding files to package...");
                    m_lastZipCurrentFilename = "";
                    zip.SaveProgress += Zip_SaveProgress;

                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestSpeed;
                    zip.Password = pwd;

                    foreach (var path in msg.Paths)
                    {
                        if (File.Exists(path))
                            zip.AddFile(path);
                        else if (Directory.Exists(path))
                            zip.AddDirectory(path);
                        else
                            throw new Exception($"Item to send not found: {path}");
                    }

                    m_app.Log("Saving package...");
                    zip.Save();
                }

                m_app.Log("Sending header...");
                long zip_file_size_bytes = new FileInfo(zip_file_path).Length;
                var send_request =
                    new ClientRequest()
                    {
                        version = 1,
                        verb = "SEND",
                        headers = new Dictionary<string, string>()
                        {
                            { "to", string.Join("; ", msg.To) },
                            { "subject", msg.Subject },
                            { "body", msg.Body },
                            { "pwd", pwd },
                            { "packageSizeBytes", zip_file_size_bytes.ToString() }
                        }
                    };
                if (m_stream == null)
                    return false;
                SecureNet.SendObject(m_stream, send_request);

                m_app.Log("Sending package...");
                using (var zip_file_stream = File.OpenRead(zip_file_path))
                {
                    int zip_file_size_mb = (int)Math.Max(zip_file_size_bytes / 1024 / 1024, 1);
                    long sent_yet = 0;
                    byte[] buffer = new byte[64 * 1024];
                    while (sent_yet < zip_file_size_bytes)
                    {
                        int to_read = (int)Math.Min(buffer.Length, zip_file_size_bytes - sent_yet);
                        int read = zip_file_stream.Read(buffer, 0, to_read);

                        m_stream.Write(buffer, 0, read);
                        sent_yet += read;

                        m_app.Progress((double)sent_yet / zip_file_size_bytes);
                        if (m_app.Cancelled)
                            return false;
                    }
                }
                if (m_app.Cancelled)
                    return false;

                m_app.Log("Receiving response...");
                var send_response = SecureNet.ReadObject<ServerResponse>(m_stream);
                m_app.Log($"Server Response: {send_response.ResponseSummary}");
                if (send_response.statusCode / 100 != 2)
                    throw send_response.CreateException();
                else
                    return true;
            }
            finally
            {
                if (File.Exists(zip_file_path))
                    File.Delete(zip_file_path);
            }
        }

        private void Zip_SaveProgress(object? sender, SaveProgressEventArgs e)
        {
            if (m_app.Cancelled)
            {
                e.Cancel = true;
                return;
            }

            if (e.CurrentEntry != null && e.CurrentEntry.FileName != m_lastZipCurrentFilename)
            {
                m_lastZipCurrentFilename = e.CurrentEntry.FileName;
                m_app.Log(m_lastZipCurrentFilename);
            }

            double min_progress =
                Math.Min
                (
                    (double)e.EntriesSaved / Math.Min(e.EntriesTotal, 1),
                    (double)e.BytesTransferred / Math.Min(e.TotalBytesToTransfer, 1)
                );
            m_app.Progress(min_progress);
        }

        private IClientApp m_app;

        private string m_lastZipCurrentFilename = "";

        public string SessionToken { get; set; } = "";

        private TcpClient? m_client = null;
        private Stream? m_stream = null;
    }
}
