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
                case "SEND":
                    return await HandleSendRequestAsync(request, ctxt).ConfigureAwait(false);
                case "POP":
                    return await HandleInboxRequestAsync(request, ctxt).ConfigureAwait(false);
                case "GET":
                    return await HandleGetRequestAsync(request, ctxt).ConfigureAwait(false);
                
                case "DOWNLOAD":
                    return await HandleDownloadRequestAsync(request, ctxt).ConfigureAwait(false);
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
                { "to", "subject", "body", "pwd", "packageHash" }
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

            long package_size_bytes = request.contentLength;
            if (package_size_bytes / 1024 / 1024 > MaxSendPayloadMB)
                throw new InputException("Header invalid: package too big");

            string sent_zip_hash = request.headers["hash"];
            if (sent_zip_hash == "")
                throw new InputException("Header missing: packageHash");

            Log(ctxt, $"Sending: To: {to} - Subject: {subject}");

            string temp_zip_file_path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
            string stored_file_path = "";
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
                string local_zip_hash =
                    await Utils.HashStreamAsync(File.OpenRead(temp_zip_file_path)).ConfigureAwait(false);
                if (local_zip_hash != sent_zip_hash)
                    throw new InputException("Received file contents do not match what was sent");

                Log(ctxt, $"Inventorying ZIP");
                int file_count = 0;
                long file_total_size_bytes = 0;
                string zip_manifest =
                    Utils.ManifestZip
                    (
                        temp_zip_file_path,
                        pwd,
                        out file_count,
                        out file_total_size_bytes
                    );
 
                Log(ctxt, $"Storing ZIP");
                stored_file_path = m_fileStore.StoreFile(temp_zip_file_path);
                File.Delete(temp_zip_file_path);
                temp_zip_file_path = "";

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
                            subject = subject,
                            body = body,
                            manifest = zip_manifest
                        },
                        stored_file_path,
                        file_count,
                        file_total_size_bytes / 1024.0 / 1024.0,
                        local_zip_hash
                    );
                }
                stored_file_path = "";

                Log(ctxt, $"Sending email");
                await ctxt.App.SendMailDeliveryMessageAsync
                (
                    email_from, 
                    to, 
                    subject, 
                    body, 
                    zip_manifest, 
                    pwd
                );
                return HandlerContext.StandardResponse;
            }
            finally
            {
                if (temp_zip_file_path != "" && File.Exists(temp_zip_file_path))
                    File.Delete(temp_zip_file_path);

                if (stored_file_path != "" && File.Exists(stored_file_path))
                    File.Delete(stored_file_path);
            }
        }

        private async Task<ServerResponse> HandleInboxRequestAsync(ClientRequest request, HandlerContext ctxt)
        {
            string to = ctxt.Auth["email"];
            m_allowBlock.EnsureEmailAllowed(to);

            Log(ctxt, $"Inbox: {to}");

            var msgs_json_stream = new MemoryStream();
            Utils.Compress
            (
                Encoding.UTF8.GetBytes
                (
                    JsonConvert.SerializeObject(m_msgStore.GetMessages(to))
                ),
                msgs_json_stream
            );

            var response =
                new ServerResponse()
                {
                    version = 1,
                    statusCode = 200,
                    statusMessage = "OK",
                    contentLength = msgs_json_stream.Length,
                    streamToSend = msgs_json_stream
                };
            await Task.FromResult(0);
            return response;
        }

        private async Task<ServerResponse> HandleGetRequestAsync(ClientRequest request, HandlerContext ctxt)
        {
            string to = ctxt.Auth["email"];
            m_allowBlock.EnsureEmailAllowed(to);

            Utils.NormalizeDict(request.headers, new[] { "token" });

            string token = request.headers["token"];
            if (token.Length == 0)
                throw new InputException("Header missing: token");

            Log(ctxt, $"Get Message: {to} - {token}");

            string package_file_path;
            var msg = m_msgStore.GetMessage(request.headers["token"], to, out package_file_path);
            if (msg == null || !File.Exists(package_file_path))
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
                        { 
                            { "msg", JsonConvert.SerializeObject(msg) }
                        }
                };
            await Task.FromResult(0);
            return response;
        }

        private async Task<ServerResponse> HandleDownloadRequestAsync(ClientRequest request, HandlerContext ctxt)
        {
            string to = ctxt.Auth["email"];
            m_allowBlock.EnsureEmailAllowed(to);

            Utils.NormalizeDict(request.headers, new[] { "token" });

            string token = request.headers["token"];
            if (token.Length == 0)
                throw new InputException("Header missing: token");

            Log(ctxt, $"Download Payload: {to} - {token}");

            string package_file_path;
            var msg = m_msgStore.GetMessage(request.headers["token"], to, out package_file_path);
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
                            { "hash", msg.fileHash }
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
