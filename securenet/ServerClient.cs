using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace msgfiles
{
    /// <summary>
    /// ServerClient manages server request processing
    /// </summary>
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
                LogRequest(auth_request);
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
                m_clientEmail = auth_request.headers["email"];

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
                    m_app.SendChallengeToken
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
                    LogRequest(auth_challenge_response);
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

                // Prepare the handle requests
                var request_handler = m_app.RequestHandler;
                var handler_ctxt = new HandlerContext(m_app, m_clientAddress, auth_request.headers, m_stream);

                while (true)
                {
                    Log("Receiving request...");
                    var client_request = await SecureNet.ReadObjectAsync<ClientRequest>(m_stream).ConfigureAwait(false);
                    LogRequest(client_request);

                    Log($"Handling request: {client_request.verb}");
                    using (var server_response = await request_handler.HandleRequestAsync(client_request, handler_ctxt).ConfigureAwait(false))
                    { 
                        Log("Sending response header...");
                        await SecureNet.SendObjectAsync(m_stream, server_response).ConfigureAwait(false);
                        Log($"Request handled: {server_response.ResponseSummary}");
                        if (server_response.streamToSend != null)
                        {
                            Log("Sending response payload...");
                            await server_response.streamToSend.CopyToAsync(m_stream, 64 * 1024).ConfigureAwait(false); ;
                            Log($"Payload sent.");
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                exp = Utils.SmashExp(exp);

                if (exp is SocketException || exp is NetworkException)
                {
                    Log($"Network error: {exp.Message}");
                }
                else if (exp is InputException)
                {
                    Log($"Input error: {exp.Message}");
                    try
                    {
                        var error_response =
                            new ServerResponse()
                            {
                                version = 1,
                                statusCode = 400,
                                statusMessage = exp.Message
                            };
                        SecureNet.SendObjectAsync(m_stream, error_response).Wait();
                    }
                    catch { }
                }
                else
                {
                    Log($"{Utils.SumExp(exp)}");
                    try
                    {
                        var error_response =
                            new ServerResponse()
                            {
                                version = 1,
                                statusCode = 500,
                                statusMessage = "Internal Server Error"
                            };
                        SecureNet.SendObjectAsync(m_stream, error_response).Wait();
                    }
                    catch { }
                }
            }
        }

        private void Log(string message)
        {
            if (m_clientEmail != "")
                m_app.Log($"{m_clientAddress} - {m_clientEmail}: {message}");
            else
                m_app.Log($"{m_clientAddress}: {message}");
        }

        private void LogRequest(ClientRequest request)
        {
            string clientEmail = m_clientEmail;
            if (string.IsNullOrEmpty(clientEmail))
            {
                if (request.headers.ContainsKey("email"))
                    clientEmail = request.headers["email"];
                else
                    clientEmail = "-";
            }

            string token;
            if (request.headers.ContainsKey("token"))
                token = request.headers["token"];
            else
                token = "-";

            m_app.LogRequest(m_clientAddress, clientEmail, request.verb, token);
        }

        private IServerApp m_app;

        private X509Certificate? m_cert;

        private TcpClient m_client;

        private string m_clientAddress;
        private string m_clientEmail = "";

        private Stream? m_stream;
    }
}
