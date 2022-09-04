using Newtonsoft.Json;

namespace msgfiles
{
    /// <summary>
    /// MsgRequestHandler implements all the message request handling
    /// </summary>
    public class MsgRequestHandler : IServerRequestHandler
    {
        public static int MaxSendPayloadMB;

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
                { "to", "message", "packageHash" }
            );

            string to = request.headers["to"];
            if (to == "")
                throw new InputException("Header missing: to");

            string message = request.headers["message"];
            if (message == "")
                throw new InputException("Header missing: message");

            long package_size_bytes = request.contentLength;
            if
            (
                MaxSendPayloadMB > 0
                &&
                package_size_bytes / 1024 / 1024 > MaxSendPayloadMB
            )
            {
                throw new InputException("Header invalid: package too big");
            }

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
                        string token = 
                            m_msgStore.StoreMessage
                            (
                                new msg()
                                {
                                    from = email_from,
                                    to = too,
                                    message = message
                                },
                                stored_file_path,
                                local_zip_hash
                            );

                            Log(ctxt, $"Sending email");
                            ctxt.App.SendDeliveryMessage
                            (
                                email_from,
                                too,
                                message,
                                token
                            );
                    }
                    stored_file_path = "";

                    return
                        new ServerResponse()
                        {
                            version = 1,
                            statusCode = 200,
                            statusMessage = "OK"
                        };
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

            Utils.NormalizeDict(request.headers, new[] { "token", "part" });

            string token = request.headers["token"];
            if (token.Length == 0)
                throw new InputException("Header missing: token");

            string part_to_get = request.headers["part"];
            if (part_to_get.Length == 0)
                throw new InputException("Header missing: part");

            bool get_msg = false, get_file = false;
            if (part_to_get == "msg")
                get_msg = true;
            else if (part_to_get == "file")
                get_file = true;
            else
                throw new InputException("Invalid header: part");

            Log(ctxt, $"Get Message: {to} - {token} - {part_to_get}");

            string package_file_path, package_file_hash;
            var msg = 
                m_msgStore.GetMessage(to, token, out package_file_path, out package_file_hash);

            if (get_msg)
            {
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

                var response =
                    new ServerResponse()
                    {
                        version = 1,
                        statusCode = 200,
                        statusMessage = "OK",
                        headers =
                            new Dictionary<string, string>()
                            { { "msg", JsonConvert.SerializeObject(msg) } },
                    };
                await Task.FromResult(0);
                return response;
            }
            else if (get_file)
            {
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
                            { { "hash", package_file_hash } },
                        streamToSend = File.OpenRead(package_file_path)
                    };
                await Task.FromResult(0);
                return response;
            }
            else
                throw new InputException("Invalid header: part");
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
