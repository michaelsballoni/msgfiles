using System.Text;

using Ionic.Zip;
using Newtonsoft.Json;

namespace msgfiles
{
    public class MsgRequestHandler : IServerRequestHandler
    {
        public static int MaxSendPayloadMB = 1024; // FORNOW - Load from config

        public MsgRequestHandler(AllowBlock allowBlock, FileStore fileStore, MessageStore msgStore)
        {
            m_allowBlock = allowBlock;
            m_fileStore = fileStore;
            m_msgStore = msgStore;
        }

        public async Task<ServerResponse> HandleRequestAsync(ClientRequest request, HandlerContext ctxt)
        {
            switch (request.verb)
            {
                case "POST":
                    return await HandleSendRequestAsync(request, ctxt).ConfigureAwait(false);
                case "GET":
                    return await HandleGetRequestAsync(request, ctxt).ConfigureAwait(false);
                case "DELETE":
                    return await HandleDeleteRequestAsync(request, ctxt).ConfigureAwait(false);
                default:
                    throw new InputException($"Unrecognized verb: {request.verb}");
            }
        }

        private void Log(HandlerContext ctxt, string msg)
        {
            ctxt.App.Log($"{ctxt.ClientAddress}:{ctxt.Auth["email"]}: {msg}");
        }

        private async Task<ServerResponse> HandleSendRequestAsync(ClientRequest request, HandlerContext ctxt)
        {
            // Unpack the message
            Utils.NormalizeDict
            (
                request.headers,
                new[]
                { "to", "message", "pwd", "packageHash" }
            );

            string to = request.headers["to"];
            if (to == "")
                throw new InputException("Header missing: to");

            string message = request.headers["message"];
            if (message == "")
                throw new InputException("Header missing: message");

            string pwd = request.headers["pwd"];
            if (pwd == "")
                throw new InputException("Header missing: pwd");

            long package_size_bytes = request.contentLength;
            if (package_size_bytes / 1024 / 1024 > MaxSendPayloadMB)
                throw new InputException("Header invalid: package too big");

            string sent_zip_hash = request.headers["hash"];
            if (sent_zip_hash == "")
                throw new InputException("Header missing: hash");

            Log(ctxt, $"Sending: To: {to}");

            using (var temp_file_use = new TempFileUse(".zip"))
            {
                string stored_file_path = "";
                string temp_zip_file_path = temp_file_use.FilePath;
                try
                {

                    Log(ctxt, $"Saving ZIP: {temp_zip_file_path}");
                    using (var zip_file_stream = File.OpenWrite(temp_zip_file_path))
                    {
                        long written_yet = 0;
                        byte[] buffer = new byte[64 * 1024];
                        while (written_yet < package_size_bytes)
                        {
                            int to_read = (int)Math.Min(package_size_bytes - written_yet, buffer.Length);
                            int read = await ctxt.ConnectionStream.ReadAsync(buffer, 0, to_read).ConfigureAwait(false);
                            if (read == 0)
                                throw new NetworkException("Connection lost");
                            await zip_file_stream.WriteAsync(buffer, 0, read).ConfigureAwait(false);
                            written_yet += read;
                        }
                    }

                    Log(ctxt, $"Hashing ZIP");
                    string local_zip_hash;
                    using (var zip_file_stream = File.OpenRead(temp_zip_file_path))
                        local_zip_hash = await Utils.HashStreamAsync(zip_file_stream).ConfigureAwait(false);
                    if (local_zip_hash != sent_zip_hash)
                        throw new InputException("Received file contents do not match what was sent");

                    Log(ctxt, $"Storing ZIP");
                    stored_file_path = m_fileStore.StoreFile(temp_zip_file_path);
                    File.Delete(temp_zip_file_path);
                    temp_zip_file_path = "";
                    temp_file_use.Clear();

                    Log(ctxt, $"Storing messages");
                    string email_from = $"{ctxt.Auth["display"]} <{ctxt.Auth["email"]}>";
                    var toos = to.Split(';').Select(t => t.Trim()).Where(t => t.Length > 0);
                    foreach (var too in toos)
                    {
                        m_msgStore.StoreMessage
                        (
                            new msg()
                            {
                                from = email_from,
                                to = too,
                                message = message
                            },
                            pwd,
                            stored_file_path,
                            local_zip_hash
                        );
                    }
                    stored_file_path = "";

                    Log(ctxt, $"Sending email");
                    await ctxt.App.SendMailDeliveryMessageAsync
                    (
                        email_from,
                        to,
                        message,
                        pwd
                    );
                    return HandlerContext.StandardResponse;
                }
                finally
                {
                    if (stored_file_path != "" && File.Exists(stored_file_path))
                        File.Delete(stored_file_path);
                }
            }
        }

        private async Task<ServerResponse> HandleGetRequestAsync(ClientRequest request, HandlerContext ctxt)
        {
            string to = ctxt.Auth["email"];
            m_allowBlock.EnsureEmailAllowed(to);

            Utils.NormalizeDict(request.headers, new[] { "pwd" });

            string pwd = request.headers["pwd"];
            if (pwd.Length == 0)
                throw new InputException("Header missing: pwd");

            Log(ctxt, $"Get Message: {to} - {pwd}");

            string package_file_path, package_file_hash;
            var msg = m_msgStore.GetMessage(to, pwd, out package_file_path, out package_file_hash);
            
            if (msg == null)
            {
                var response_404 =
                    new ServerResponse()
                    {
                        version = 1,
                        statusCode = 404,
                        statusMessage = "Message Not Found"
                    };
                return response_404;
            }

            if (!File.Exists(package_file_path))
            {
                var response_404 =
                    new ServerResponse()
                    {
                        version = 1,
                        statusCode = 404,
                        statusMessage = "File Not Found"
                    };
                return response_404;
            }

            var response =
                new ServerResponse()
                {
                    version = 1,
                    statusCode = 200,
                    statusMessage = "OK",
                    contentLength = new FileInfo(package_file_path).Length,
                    headers =
                        new Dictionary<string, string>()
                        {
                            { "msg", JsonConvert.SerializeObject(msg) },
                            { "hash", package_file_hash }
                        },
                    streamToSend = File.OpenRead(package_file_path)
                };
            await Task.FromResult(0);
            return response;
        }

        private async Task<ServerResponse> HandleDeleteRequestAsync(ClientRequest request, HandlerContext ctxt)
        {
            string to = ctxt.Auth["email"];
            m_allowBlock.EnsureEmailAllowed(to);

            Utils.NormalizeDict(request.headers, new[] { "token" });
            string token = request.headers["token"];
            if (token.Length == 0)
                throw new InputException("Header missing: token");

            Log(ctxt, $"Delete Message: {to} - {token}");

            ServerResponse response;
            if (!m_msgStore.DeleteMessage(token, to))
            {
                response =
                    new ServerResponse()
                    {
                        version = 1,
                        statusCode = 404,
                        statusMessage = "Message Not Found"
                    };
            }
            else
            {
                response =
                    new ServerResponse()
                    {
                        version = 1,
                        statusCode = 204,
                        statusMessage = "Message Deleted"
                    };
            }
            await Task.FromResult(0);
            return response;
        }

        private AllowBlock m_allowBlock;
        private FileStore m_fileStore;
        private MessageStore m_msgStore;
    }
}
