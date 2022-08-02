using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using Ionic.Zip;

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
                    new_client.NoDelay = true;
                    new_client.ReceiveTimeout = 900 * 1000;

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
            string client_address;
            {
                string? client_address_null =
                    client.Client.RemoteEndPoint == null ? "<unknown>" : client.Client.RemoteEndPoint.ToString();
                client_address = client_address_null ?? "<null>";
            }

            try
            {
                LogClient(client_address, "Securing new connection...");
                lock (m_clients)
                    m_clients.Add(client);
                using (Stream stream = await SecureNet.SecureConnectionFromClient(client, m_cert).ConfigureAwait(false))
                {
                    try
                    {
                        LogClient(client_address, "Receiving auth request...");
                        var auth_request = await SecureNet.ReadObjectAsync<ClientRequest>(stream).ConfigureAwait(false);
                        LogClient(client_address, "Auth request received.");
                        if (auth_request.verb != "AUTH")
                            throw new InputException("Verb should be AUTH");
                        Utils.NormalizeDict(auth_request.headers, new[] { "display", "email", "session" });
                        if
                        (
                            string.IsNullOrEmpty(auth_request.headers["display"])
                            ||
                            string.IsNullOrEmpty(auth_request.headers["email"])
                        )
                        {
                            throw new InputException("Auth request missing fields");
                        }

                        Session? session = m_app.GetSession(auth_request.headers);
                        if 
                        (
                            session != null 
                            && 
                            (
                                session.email != auth_request.headers["email"]
                                ||
                                session.display != auth_request.headers["display"]
                            )
                        )
                        {
                            m_app.DropSession(auth_request.headers);
                            session = null;
                        }
                        if (session == null)
                        {
                            LogClient(client_address, "Sending challenge token...");
                            string challenge_token = Utils.GenChallenge();
                            await m_app.SendChallengeTokenAsync
                            (
                                auth_request.headers["email"], 
                                auth_request.headers["display"], 
                                challenge_token
                            );
                            LogClient(client_address, "Challenge token sent.");

                            LogClient(client_address, "Sending challenge response...");
                            var auth_challenge =
                                new ServerResponse()
                                {
                                    statusCode = 401,
                                    statusMessage = "Challenge Response Required"
                                };
                            await SecureNet.SendObjectAsync(stream, auth_challenge).ConfigureAwait(false);
                            LogClient(client_address, "Challenge response sent.");

                            LogClient(client_address, "Receiving challenge response...");
                            var auth_challenge_response = await SecureNet.ReadObjectAsync<ClientRequest>(stream).ConfigureAwait(false);
                            LogClient(client_address, "Challenge response received.");
                            if (auth_challenge_response.verb != "CHALLENGE")
                                throw new InputException("Verb should be CHALLENGE");
                            Utils.NormalizeDict(auth_challenge_response.headers, new[] { "challenge" });
                            if (auth_challenge_response.headers["challenge"] != challenge_token)
                                throw new InputException("Incorrect challenge response");

                            LogClient(client_address, "Creating session...");
                            session = m_app.CreateSession(auth_request.headers);
                        }

                        LogClient(client_address, "Sending session...");
                        var auth_response =
                            new ServerResponse()
                            {
                                version = 1,
                                statusCode = 200,
                                statusMessage = "OK",
                                headers = new Dictionary<string, string>() { { "session", session.token } }
                            };
                        await SecureNet.SendObjectAsync(stream, auth_response).ConfigureAwait(false);
                        LogClient(client_address, "Session sent.  Client authenticated, ready for operations.");

                        while (true)
                        {
                            LogClient(client_address, "Receiving request...");
                            var client_request = await SecureNet.ReadObjectAsync<ClientRequest>(stream).ConfigureAwait(false);
                            
                            switch (client_request.verb)
                            {
                                case "SENDMESSAGE":
                                    // Unpack the message
                                    Utils.NormalizeDict
                                    (
                                        client_request.headers,
                                        new[]
                                        { "to", "subject", "body", "pwd", "packageSizeBytes" }
                                    );

                                    string to = client_request.headers["to"];
                                    if (to == "")
                                        throw new InputException("Header missing: to");

                                    string subject = client_request.headers["subject"];
                                    if (subject == "")
                                        throw new InputException("Header missing: subject");

                                    string body = client_request.headers["body"];
                                    if (body == "")
                                        throw new InputException("Header missing: body");

                                    string pwd = client_request.headers["pwd"];
                                    if (pwd == "")
                                        throw new InputException("Header missing: pwd");

                                    long package_size_bytes;
                                    if (!long.TryParse(client_request.headers["packageSizeBytes"], out package_size_bytes))
                                        throw new InputException("Header missing: packageSizeBytes");
                                    if (package_size_bytes > int.MaxValue)
                                        throw new InputException("Header invalid: packageSizeBytes > 2 GB");

                                    // Save the ZIP to disk
                                    string temp_zip_file_path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
                                    using (var zip_file_stream = File.OpenWrite(temp_zip_file_path))
                                    {
                                        long written_yet = 0;
                                        byte[] buffer = new byte[64 * 1024];
                                        while (written_yet < package_size_bytes)
                                        {
                                            int to_read = (int)Math.Min(package_size_bytes - written_yet, buffer.Length);
                                            int read = await stream.ReadAsync(buffer, 0, to_read).ConfigureAwait(false);
                                            if (read == 0)
                                                throw new NetworkException("Connection lost");
                                            await zip_file_stream.WriteAsync(buffer, 0, read).ConfigureAwait(false);
                                            written_yet += read;
                                        }
                                    }

                                    string zip_manifest;
                                    {
                                        StringBuilder sb = new StringBuilder();
                                        using (var zip_file = new ZipFile(temp_zip_file_path))
                                        {
                                            zip_file.Password = pwd;
                                            foreach (var zip_entry in zip_file.Entries)
                                            {
                                                string size_str;
                                                {
                                                    long size = zip_entry.UncompressedSize;
                                                    if (size > 1024 * 1024 * 1024)
                                                        size_str = $"{Math.Round((double)size / 1024 / 1024 / 1024, 1)} GB";
                                                    else if (size > 1024 * 1024)
                                                        size_str = $"{Math.Round((double)size / 1024 / 1024, 1)} MB";
                                                    else if (size > 1024)
                                                        size_str = $"{Math.Round((double)size / 1024, 1)} KB";
                                                    else
                                                        size_str = $"{size} bytes";
                                                }
                                                sb.AppendLine($"{zip_entry.FileName} ({size_str})");
                                            }
                                        }
                                        zip_manifest = sb.ToString();
                                    }

                                    // FORNOW - Finish this!!!
                                    break;

                                default:
                                    throw new InputException($"Unrecognized verb: {client_request.verb}");
                            }

                            var server_response =
                                new ServerResponse()
                                {
                                    version = 1,
                                    statusCode = 200,
                                    statusMessage = "OK",
                                    headers = new Dictionary<string, string>()
                                };
                            await SecureNet.SendObjectAsync(stream, server_response).ConfigureAwait(false);
                        }
                    }
                    catch (SocketException)
                    {
                        LogClient(client_address, "Socket Exception");
                    }
                    catch (NetworkException exp)
                    {
                        LogClient(client_address, $"{Utils.SumExp(exp)}");
                    }
                    catch (Exception exp)
                    {
                        exp = Utils.SmashExp(exp);
                        bool is_server_exp = exp is ServerException;
                        LogClient(client_address, $"{Utils.SumExp(exp)}");
                        try
                        {
                            var error_response =
                                new ServerResponse()
                                {
                                    version = 1,
                                    statusCode = is_server_exp ? 500 : 400,
                                    statusMessage = is_server_exp ? "Internal Server Error" : exp.Message,
                                    headers = new Dictionary<string, string>()
                                };
                            SecureNet.SendObjectAsync(stream, error_response).Wait();
                        }
                        catch { }
                    }
                }
            }
            catch (Exception exp)
            {
                LogClient(client_address, $"HandleClient ERROR: {Utils.SumExp(exp)}");
            }
            finally
            {
                try { client.Dispose(); } catch { }
                lock (m_clients)
                    m_clients.Remove(client);
            }
        }

        private void LogClient(string clientAddress, string message)
        {
            m_app.Log($"{clientAddress}: {message}");
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
