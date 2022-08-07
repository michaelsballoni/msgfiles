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
                    {
                        ServerResponse server_response = 
                            await HandleSendRequestAsync(request, ctxt).ConfigureAwait(false);
                        return server_response;
                    }

                case "POP":
                    {
                        ServerResponse server_response =
                            await HandleInboxRequestAsync(request, ctxt).ConfigureAwait(false);
                        return server_response;
                    }

                case "GET":
                    {
                        ServerResponse server_response =
                            await HandleGetRequestAsync(request, ctxt).ConfigureAwait(false);
                        return server_response;
                    }

                case "DELETE":
                    {
                        ServerResponse server_response =
                            await HandleDeleteRequestAsync(request, ctxt).ConfigureAwait(false);
                        return server_response;
                    }

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
            if (package_size_bytes / 1024 / 1024 > MaxSendPayloadMB)
                throw new InputException("Header invalid: package too big");

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

                Log(ctxt, $"Inventorying ZIP");
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
                        stored_file_path
                    );
                }
                stored_file_path = "";

                Log(ctxt, $"Sending email");
                string email_body =
                    $"msgfiles from {email_from}: {subject}\n\n" +
                    $"{body}\n\n" +
                    $"Run the msgfiles application, open this message there, and enter this password:\n\n" +
                    $"Password: {pwd}\n\n" +
                    $"If you do not recogize the sender or anything looks suspicious in this message or the list of files below, reply to this email to report it.\n\n" +
                    $"Here are the files you have been sent.\n\n" +
                    zip_manifest;
                await ctxt.App.SendEmailAsync(email_from, to, email_body);

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

            var msgs = m_msgStore.GetMessages(to);

            var msgs_json = JsonConvert.SerializeObject(msgs);
            // FORNOW - Compress the JSON
            // FORNOW - Write the JSON to the output stream, a MemoryStream

            var response =
                new ServerResponse()
                {
                    version = 1,
                    statusCode = 200,
                    statusMessage = "OK",
                    headers = new Dictionary<string, string>() { }
                };
            await Task.FromResult(0);
            return response;
        }

        private async Task<ServerResponse> HandleGetRequestAsync(ClientRequest request, HandlerContext ctxt)
        {
            string to = ctxt.Auth["email"];
            m_allowBlock.EnsureEmailAllowed(to);

            Utils.NormalizeDict(request.headers, new[] { "token" });

            string package_file_path;
            var msg = m_msgStore.GetMessage(request.headers["token"], to, out package_file_path);
            if (msg == null)
            {
                var response_404 =
                    new ServerResponse()
                    {
                        version = 1,
                        statusCode = 404,
                        statusMessage = "Message Not Found",
                        headers = new Dictionary<string, string>()
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
                        statusMessage = "File Not Found",
                        headers = new Dictionary<string, string>()
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
                            { "msg", JsonConvert.SerializeObject(msg) },
                            { "fileLength", new FileInfo(package_file_path).Length.ToString() }
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

            ServerResponse response;
            if (!m_msgStore.DeleteMessage(request.headers["token"], to))
            {
                response =
                    new ServerResponse()
                    {
                        version = 1,
                        statusCode = 404,
                        statusMessage = "Message Not Found",
                        headers = new Dictionary<string, string>()
                    };
            }
            else
            {
                response =
                    new ServerResponse()
                    {
                        version = 1,
                        statusCode = 204,
                        statusMessage = "Message Deleted",
                        headers = new Dictionary<string, string>()
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
