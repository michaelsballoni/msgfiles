using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

using Ionic.Zip;

namespace msgfiles
{
    internal class ServerClient : IDisposable
    {
        public ServerClient(IServerApp app, X509Certificate cert, TcpClient client)
        {
            m_app = app;
            m_cert = cert;

            m_client = client;

            {
                string? client_address_null =
                    client.Client.RemoteEndPoint == null ? "<unknown>" : client.Client.RemoteEndPoint.ToString();
                m_clientAddress = client_address_null ?? "<null>";
            }
        }

        public void Dispose()
        {
            if (m_stream != null)
            {
                try { m_stream.Dispose(); } catch { }
                m_stream = null;
            }
        }

        public async Task HandleClientAsync()
        {
            Log("Securing new connection...");
            if (m_cert == null)
                throw new NullReferenceException("m_cert");
            m_stream = await SecureNet.SecureConnectionFromClient(m_client, m_cert).ConfigureAwait(false);
            try
            {
                Log("Receiving auth request...");
                var auth_request = await SecureNet.ReadObjectAsync<ClientRequest>(m_stream).ConfigureAwait(false);
                Log("Auth request received.");
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
                    Log("Sending challenge token...");
                    string challenge_token = Utils.GenChallenge();
                    await m_app.SendChallengeTokenAsync
                    (
                        auth_request.headers["email"],
                        auth_request.headers["display"],
                        challenge_token
                    );
                    Log("Challenge token sent.");

                    Log("Sending challenge response...");
                    var auth_challenge =
                        new ServerResponse()
                        {
                            statusCode = 401,
                            statusMessage = "Challenge Response Required"
                        };
                    await SecureNet.SendObjectAsync(m_stream, auth_challenge).ConfigureAwait(false);
                    Log("Challenge response sent.");

                    Log("Receiving challenge response...");
                    var auth_challenge_response = await SecureNet.ReadObjectAsync<ClientRequest>(m_stream).ConfigureAwait(false);
                    Log("Challenge response received.");
                    if (auth_challenge_response.verb != "CHALLENGE")
                        throw new InputException("Verb should be CHALLENGE");
                    Utils.NormalizeDict(auth_challenge_response.headers, new[] { "challenge" });
                    if (auth_challenge_response.headers["challenge"] != challenge_token)
                        throw new InputException("Incorrect challenge response");

                    Log("Creating session...");
                    session = m_app.CreateSession(auth_request.headers);
                }

                Log("Sending session...");
                var auth_response =
                    new ServerResponse()
                    {
                        version = 1,
                        statusCode = 200,
                        statusMessage = "OK",
                        headers = new Dictionary<string, string>() { { "session", session.token } }
                    };
                await SecureNet.SendObjectAsync(m_stream, auth_response).ConfigureAwait(false);
                Log("Session sent.  Client authenticated, ready for operations.");

                while (true)
                {
                    Log("Receiving request...");
                    var client_request = await SecureNet.ReadObjectAsync<ClientRequest>(m_stream).ConfigureAwait(false);

                    Log("Handling request...");
                    var server_response = await HandleRequestAsync(client_request).ConfigureAwait(false);
                    if (server_response == null)
                    {
                        server_response =
                            new ServerResponse()
                            {
                                version = 1,
                                statusCode = 200,
                                statusMessage = "OK",
                                headers = new Dictionary<string, string>()
                            };
                    }

                    Log("Sending response...");
                    await SecureNet.SendObjectAsync(m_stream, server_response).ConfigureAwait(false);
                }
            }
            catch (SocketException)
            {
                Log("Socket Exception");
            }
            catch (NetworkException exp)
            {
                Log($"{Utils.SumExp(exp)}");
            }
            catch (Exception exp)
            {
                exp = Utils.SmashExp(exp);
                bool is_input_exp = exp is InputException;
                Log($"{Utils.SumExp(exp)}");
                try
                {
                    var error_response =
                        new ServerResponse()
                        {
                            version = 1,
                            statusCode = is_input_exp ? 400 : 500,
                            statusMessage = is_input_exp ? exp.Message : "Internal Server Error",
                            headers = new Dictionary<string, string>()
                        };
                    SecureNet.SendObjectAsync(m_stream, error_response).Wait();
                }
                catch { }
            }
        }

        private async Task<ServerResponse?> HandleRequestAsync(ClientRequest request)
        {
            ServerResponse? server_response = null;
            switch (request.verb)
            {
                case "SENDMESSAGE":
                    server_response = await HandleSendRequestAsync(request).ConfigureAwait(false);
                    break;

                default:
                    throw new InputException($"Unrecognized verb: {request.verb}");
            }
            return server_response;
        }

        private async Task<ServerResponse?> HandleSendRequestAsync(ClientRequest request)
        {
            if (m_stream == null)
                throw new NullReferenceException("m_stream");

            // Unpack the message
            Utils.NormalizeDict
            (
                request.headers,
                new[]
                { "to", "subject", "body", "pwd", "packageSizeBytes" }
            );

            string to = request.headers["to"];
            if (to == "")
                throw new InputException("Header missing: to");

            string subject = request.headers["subject"];
            if (subject == "")
                throw new InputException("Header missing: subject");

            string body = request.headers["body"];
            if (body == "")
                throw new InputException("Header missing: body");

            string pwd = request.headers["pwd"];
            if (pwd == "")
                throw new InputException("Header missing: pwd");

            long package_size_bytes;
            if (!long.TryParse(request.headers["packageSizeBytes"], out package_size_bytes))
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
                    int read = await m_stream.ReadAsync(buffer, 0, to_read).ConfigureAwait(false);
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

            return null; // standard response is fine
        }

        private void Log(string message)
        {
            m_app.Log($"{m_clientAddress}: {message}");
        }

        private IServerApp m_app;

        private X509Certificate? m_cert;

        private TcpClient m_client;
        private string m_clientAddress;

        private Stream? m_stream;
    }
}
