using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ionic.Zip;

namespace msgfiles
{
    public class MsgRequestHandler : IServerRequestHandler
    {
        public static int MaxSendPayloadMB = 1024; // FORNOW - Load from config

        public MsgRequestHandler(FileStore fileStore)
        {
            m_fileStore = fileStore;
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

                case "INBOX":
                    {
                        ServerResponse server_response =
                            await HandleInboxRequestAsync(request, ctxt).ConfigureAwait(false);
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

            // Save the ZIP to disk
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

                // FORNOW
                // Store the ZIP file, key -> path
                // Store the messages to the to addresses

                Log(ctxt, $"Sending email");

                string email_body =
                    $"msgfiles from {ctxt.Auth["display"]} ({ctxt.Auth["email"]}): {subject}\n\n" +
                    $"{body}\n\n" +
                    $"Run the msgfiles application, open this message there, and enter this password to access these files:\n\n" +
                    $"Password: {pwd}\n\n" +
                    $"If you do not recogize the sender or anything looks suspicious in the list of files below, reply to this email to report it.\n\n" +
                    $"Here are the files you have been sent.\n\n" +
                    zip_manifest;

                string email_from = $"{ctxt.Auth["display"]} <{ctxt.Auth["email"]}>";
                await ctxt.App.SendMessageAsync(email_from, to, email_body);

                return HandlerContext.StandardResponse;
            }
            finally
            {
                if (temp_zip_file_path != "" && File.Exists(temp_zip_file_path))
                    File.Delete(temp_zip_file_path);
            }
        }

        private async Task<ServerResponse> HandleInboxRequestAsync(ClientRequest request, HandlerContext ctxt)
        {
            string to = ctxt.Auth["email"];
            m_allowBlock.EnsureEmailAllowed();
        }

        private AllowBlock m_allowBlock;
        private FileStore m_fileStore;
    }
}
