using System.Text;

using Ionic.Zip;
using Newtonsoft.Json;

namespace msgfiles
{
    public class MsgClient : Client
    {
        public MsgClient(IClientApp app)
            : base(app)
        {
        }

        public bool SendMsg(List<string> to, string subject, string body, List<string> paths)
        {
            string pwd = Utils.GenToken();
            string zip_file_path =
                Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
            try
            {
                using (var zip = new ZipFile(zip_file_path))
                {
                    App.Log("Adding files to package...");
                    m_lastZipCurrentFilename = "";
                    zip.SaveProgress += Zip_SaveProgress;

                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestSpeed;
                    zip.Password = pwd;

                    foreach (var path in paths)
                    {
                        if (File.Exists(path))
                            zip.AddFile(path);
                        else if (Directory.Exists(path))
                            zip.AddDirectory(path);
                        else
                            throw new Exception($"Item to send not found: {path}");
                    }

                    App.Log("Saving package...");
                    zip.Save();
                }

                App.Log("Scanning package...");
                string zip_hash = Utils.HashStream(File.OpenRead(zip_file_path));

                App.Log("Sending message...");
                long zip_file_size_bytes = new FileInfo(zip_file_path).Length;
                var send_request =
                    new ClientRequest()
                    {
                        version = 1,
                        verb = "SEND",
                        contentLength = zip_file_size_bytes,
                        headers = new Dictionary<string, string>()
                        {
                            { "to", string.Join("; ", to) },
                            { "subject", subject },
                            { "body", body },
                            { "pwd", pwd },
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
                        int to_read = (int)Math.Min(buffer.Length, zip_file_size_bytes - sent_yet);
                        int read = zip_file_stream.Read(buffer, 0, to_read);

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
                    else
                        return true;
                }
            }
            finally
            {
                if (File.Exists(zip_file_path))
                    File.Delete(zip_file_path);
            }
        }

        public List<msg> GetMessages()
        {
            App.Log("Sending POP request...");
            var request =
                new ClientRequest()
                {
                    version = 1,
                    verb = "POP"
                };
            if (ServerStream == null)
                throw new NullReferenceException("ServerStream");
            SecureNet.SendObject(ServerStream, request);

            App.Log("Receiving POP response...");
            using (var response = SecureNet.ReadObject<ServerResponse>(ServerStream))
            {
                App.Log($"Server Response: {response.ResponseSummary}");
                if (response.statusCode / 100 != 2)
                    throw response.CreateException();

                int total_to_read = (int)response.contentLength;
                byte[] inbox_bytes = new byte[total_to_read];
                int read_yet = 0;
                while (read_yet < total_to_read)
                {
                    int read = ServerStream.Read(inbox_bytes, read_yet, total_to_read - read_yet);
                    if (read == 0)
                        throw new NetworkException("Connection lost");
                    read_yet += read;
                }

                List<msg>? inbox_msgs =
                    JsonConvert.DeserializeObject<List<msg>>
                    (
                        Encoding.UTF8.GetString(Utils.Decompress(inbox_bytes))
                    );
                if (inbox_msgs == null)
                    throw new NetworkException("Processing response failed");
                
                App.Log($"Messages: {inbox_msgs.Count}");
                return inbox_msgs;
            }
        }

        public msg GetMessage(string token)
        {
            App.Log("Sending GET request...");
            var request =
                new ClientRequest()
                {
                    version = 1,
                    verb = "GET",
                    headers = 
                        new Dictionary<string, string>()
                        { { "token", token } }
                };
            if (ServerStream == null)
                throw new NullReferenceException("ServerStream");
            SecureNet.SendObject(ServerStream, request);

            App.Log("Receiving GET response...");
            using (var response = SecureNet.ReadObject<ServerResponse>(ServerStream))
            {
                App.Log($"Server Response: {response.ResponseSummary}");
                if (response.statusCode / 100 != 2)
                    throw response.CreateException();

                msg? m = JsonConvert.DeserializeObject<msg>(response.headers["msg"]);
                if (m == null)
                    throw new NetworkException("Processing response failed");
                App.Log($"Message: {m.subject}");
                return m;
            }
        }

        public string DownloadMessage(string token)
        {
            App.Log("Sending DOWNLOAD request...");
            var request =
                new ClientRequest()
                {
                    version = 1,
                    verb = "DOWNLOAD",
                    headers = 
                        new Dictionary<string, string>()
                        { { "token", token } }
                };
            if (ServerStream == null)
                throw new NullReferenceException("ServerStream");
            SecureNet.SendObject(ServerStream, request);

            App.Log("Receiving DOWNLOAD response...");
            using (var response = SecureNet.ReadObject<ServerResponse>(ServerStream))
            {
                App.Log($"Server Response: {response.ResponseSummary}");
                if (response.statusCode / 100 != 2)
                    throw response.CreateException();

                string temp_file_path = Path.Combine(Path.GetTempPath(), $"{token}.zip");
                try
                {
                    using (var fs = File.OpenWrite(temp_file_path))
                    {
                        long total_to_read = response.contentLength;
                        long read_yet = 0;
                        byte[] buffer = new byte[64 * 1024];
                        while (read_yet < total_to_read)
                        {
                            int to_read = (int)Math.Min(total_to_read - read_yet, buffer.Length);
                            int read = ServerStream.Read(buffer, 0, to_read);
                            if (read == 0)
                                throw new NetworkException("Connection lost");
                            fs.Write(buffer, 0, read);
                            read_yet += read;
                        }

                        fs.Seek(0, SeekOrigin.Begin);
                        string local_hash = Utils.HashStream(fs);
                        fs.Seek(0, SeekOrigin.Begin);
                        if (local_hash != response.headers["hash"])
                            throw new NetworkException("File transmission corrupted");

                        string ret_val = temp_file_path;
                        temp_file_path = "";
                        return ret_val;
                    }
                }
                finally
                {
                    if (temp_file_path != "" && File.Exists(temp_file_path))
                        File.Delete(temp_file_path);
                }
            }
        }

        public void DeleteMessage(string token)
        {
            App.Log("Sending DELETE request...");
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
                throw new NullReferenceException("ServerStream");
            SecureNet.SendObject(ServerStream, request);

            App.Log("Receiving DELETE response...");
            using (var response = SecureNet.ReadObject<ServerResponse>(ServerStream))
            {
                App.Log($"Server Response: {response.ResponseSummary}");
                if (response.statusCode / 100 != 2)
                    throw response.CreateException();
            }
        }

        private void Zip_SaveProgress(object? sender, SaveProgressEventArgs e)
        {
            if (App.Cancelled)
            {
                e.Cancel = true;
                return;
            }

            if (e.CurrentEntry != null && e.CurrentEntry.FileName != m_lastZipCurrentFilename)
            {
                m_lastZipCurrentFilename = e.CurrentEntry.FileName;
                App.Log(m_lastZipCurrentFilename);
            }

            double min_progress =
                Math.Min
                (
                    (double)e.EntriesSaved / Math.Min(e.EntriesTotal, 1),
                    (double)e.BytesTransferred / Math.Min(e.TotalBytesToTransfer, 1)
                );
            App.Progress(min_progress);
        }

        private string m_lastZipCurrentFilename = "";
    }
}
