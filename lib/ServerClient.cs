using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;


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

                // Prepare the handle requests
                var request_handler = m_app.RequestHandler;
                var handler_ctxt = new HandlerContext(m_stream);

                while (true)
                {
                    Log("Receiving request...");
                    var client_request = await SecureNet.ReadObjectAsync<ClientRequest>(m_stream).ConfigureAwait(false);

                    Log("Handling request...");
                    var server_response = await request_handler.HandleRequestAsync(client_request, handler_ctxt).ConfigureAwait(false);

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
