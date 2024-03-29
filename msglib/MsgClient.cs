﻿using System.Text;

using Newtonsoft.Json;

namespace msgfiles
{
    /// <summary>
    /// MsgClient implements the client application's functionality
    /// The application simply handles pacification and user prompts
    /// </summary>
    public class MsgClient : Client
    {
        public MsgClient(IClientApp app) : base(app) { }

        /// <summary>
        /// Send a message with files to recipients
        /// </summary>
        public bool SendMsg
        (
            IEnumerable<string> to, 
            string message, 
            IEnumerable<string> paths
        )
        {
            using (var temp_file_use = new TempFileUse(".zip"))
            {
                string zip_file_path = temp_file_use.FilePath;

                App.Log("Adding files to package...");
                Utils.CreateZip(App, zip_file_path, paths);

                App.Log("Scanning package...");
                string zip_hash;
                using (var fs = File.OpenRead(zip_file_path))
                    zip_hash = Utils.HashStream(fs);

                App.Log("Sending message...");
                long zip_file_size_bytes = new FileInfo(zip_file_path).Length;
                var send_request =
                    new ClientRequest()
                    {
                        version = 1,
                        verb = "POST",
                        contentLength = zip_file_size_bytes,
                        headers = new Dictionary<string, string>()
                        {
                            { "to", string.Join("; ", to) },
                            { "message", message },
                            { "hash", zip_hash }
                        }
                    };
                if (ServerStream == null)
                    return false;
                SecureNet.SendObject(ServerStream, send_request);

                App.Log("Sending package...");
                using (var zip_file_stream = File.OpenRead(zip_file_path))
                {
                    long sent_yet = 0;
                    byte[] buffer = new byte[64 * 1024];
                    while (sent_yet < zip_file_size_bytes)
                    {
                        int to_read = (int)Math.Min(zip_file_size_bytes - sent_yet, buffer.Length);
                        int read = zip_file_stream.Read(buffer, 0, to_read);
                        if (App.Cancelled)
                            return false;

                        if (ServerStream == null)
                            return false;
                        ServerStream.Write(buffer, 0, read);

                        sent_yet += read;

                        App.Progress((double)sent_yet / zip_file_size_bytes);
                        if (App.Cancelled)
                            return false;
                    }
                }
                if (App.Cancelled)
                    return false;

                App.Log("Receiving response...");
                using (var send_response = SecureNet.ReadObject<ServerResponse>(ServerStream))
                {
                    App.Log($"Server Response: {send_response.ResponseSummary}");
                    if (send_response.statusCode / 100 != 2)
                        throw send_response.CreateException();
                }

                return true;
            }
        }

        /// <summary>
        /// Given a message token, get a message for the current user
        /// Returns true if getting the message succeeded
        /// Sets shouldDelete to true if the user canceled the operation
        /// </summary>
        public bool GetMessage(string msgToken, out bool shouldDelete)
        {
            shouldDelete = false;

            {
                App.Log("Sending GET msg request...");
                var request =
                    new ClientRequest()
                    {
                        version = 1,
                        verb = "GET",
                        headers =
                            new Dictionary<string, string>()
                            { { "token", msgToken }, { "part", "msg"} }
                    };
                if (ServerStream == null)
                    return false;
                SecureNet.SendObject(ServerStream, request);
                if (App.Cancelled)
                    return false;

                App.Log("Receiving GET msg response...");
                if (ServerStream == null)
                    return false;
                using (var response = SecureNet.ReadObject<ServerResponse>(ServerStream))
                {
                    App.Log($"Server Response: {response.ResponseSummary}");
                    if (response.statusCode / 100 != 2)
                        throw response.CreateException();

                    msg? m = JsonConvert.DeserializeObject<msg>(response.headers["msg"]);
                    string status = m == null ? "(null)" : m.from;
                    App.Log($"Message: {status}");
                    if (m == null)
                        return false;
                    else
                        msgToken = m.token;

                    if (!App.ConfirmDownload(m.from, m.message, out shouldDelete))
                        return false;
                }
            }

            {
                App.Log("Sending GET file request...");
                var request =
                    new ClientRequest()
                    {
                        version = 1,
                        verb = "GET",
                        headers =
                            new Dictionary<string, string>()
                            { { "token", msgToken }, { "part", "file"} }
                    };
                if (ServerStream == null)
                    return false;
                SecureNet.SendObject(ServerStream, request);
                if (App.Cancelled)
                    return false;

                App.Log("Receiving GET file response...");
                if (ServerStream == null)
                    return false;
                using (var response = SecureNet.ReadObject<ServerResponse>(ServerStream))
                {
                    App.Log($"Server Response: {response.ResponseSummary}");
                    if (response.statusCode / 100 != 2)
                        throw response.CreateException();

                    using (var temp_file_use = new TempFileUse(".zip"))
                    {
                        string temp_file_path = temp_file_use.FilePath;

                        App.Log($"Downloading files...");
                        if (App.Cancelled)
                            return false;
                        using (var fs = File.OpenWrite(temp_file_path))
                        {
                            long total_to_read = response.contentLength;
                            long read_yet = 0;
                            byte[] buffer = new byte[64 * 1024];
                            while (read_yet < total_to_read)
                            {
                                int to_read = (int)Math.Min(total_to_read - read_yet, buffer.Length);
                                if (ServerStream == null)
                                    return false;
                                int read = ServerStream.Read(buffer, 0, to_read);
                                if (App.Cancelled)
                                    return false;

                                if (read == 0)
                                    throw new NetworkException("Connection lost");
                                fs.Write(buffer, 0, read);
                                if (App.Cancelled)
                                    return false;

                                read_yet += read;

                                App.Progress((double)read_yet / total_to_read);
                            }
                        }

                        App.Log($"Scanning downloaded files...");
                        if (App.Cancelled)
                            return false;
                        string local_hash;
                        using (var fs = File.OpenRead(temp_file_path))
                            local_hash = Utils.HashStream(fs);
                        if (App.Cancelled)
                            return false;
                        if (local_hash != response.headers["hash"])
                            throw new NetworkException("File transmission error");

                        App.Log($"Examining downloaded files...");
                        string manifest = Utils.ManifestZip(temp_file_path);
                        if (App.Cancelled)
                            return false;

                        string extraction_dir_path = "";
                        if (!App.ConfirmExtraction(manifest, out shouldDelete, out extraction_dir_path))
                            return false;

                        App.Log($"Saving downloaded files...");
                        Utils.ExtractZip(App, temp_file_path, extraction_dir_path);

                        App.Log($"All done.");
                        return true;
                    }
                }
            }
        }

        /// <summary>
        /// Delete a message given its token
        /// </summary>
        public bool DeleteMessage(string token)
        {
            App.Log("Deleting message...");
            var request =
                new ClientRequest()
                {
                    version = 1,
                    verb = "DELETE",
                    headers =
                        new Dictionary<string, string>()
                        { { "token", token } }
                };
            if (ServerStream == null)
                return false;
            SecureNet.SendObject(ServerStream, request);

            App.Log("Receiving confirmation of delete...");
            if (ServerStream == null)
                return false;
            using (var response = SecureNet.ReadObject<ServerResponse>(ServerStream))
            {
                App.Log($"Server Response: {response.ResponseSummary}");
                if (response.statusCode / 100 != 2)
                    throw response.CreateException();
            }
            return true;
        }
    }
}
