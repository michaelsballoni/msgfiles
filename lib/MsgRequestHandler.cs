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
        public async Task<ServerResponse> HandleRequestAsync(ClientRequest request, HandlerContext ctxt)
        {
            switch (request.verb)
            {
                case "SENDMESSAGE":
                    {
                        ServerResponse server_response = 
                            await HandleSendRequestAsync(request, ctxt).ConfigureAwait(false);
                        return server_response;
                    }

                default:
                    throw new InputException($"Unrecognized verb: {request.verb}");
            }
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
            if (package_size_bytes > int.MaxValue)
                throw new InputException("Header invalid: packageSizeBytes > 2 GB");

            // Save the ZIP to disk
            string temp_zip_file_path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
            try
            {
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

                return HandlerContext.StandardResponse;
            }
            finally
            {
                if (File.Exists(temp_zip_file_path))
                    File.Delete(temp_zip_file_path);
            }
        }
    }
}
