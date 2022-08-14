using System.Text;

using Ionic.Zip;
using Newtonsoft.Json;

namespace msgfiles
{
    public class MsgClient : Client
    {
        public MsgClient(IClientApp app) : base(app) { }

        public bool SendMsg
        (
            IEnumerable<string> to, 
            string subject, 
            string body, 
            IEnumerable<string> paths
        )
        {
            string pwd = Utils.GenToken().Substring(0, 32);
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
                            zip.AddFile(path, "");
                        else if (Directory.Exists(path))
                            zip.AddDirectory(path, Path.GetFileName(path));
                        else
                            throw new InputException($"Item to send not found: {path}");
                    }

                    App.Log("Saving package...");
                    zip.Save();
                }

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
            finally
            {
                if (File.Exists(zip_file_path))
                    File.Delete(zip_file_path);
            }
        }

        public bool GetMessage(string pwd, out string token, out bool shouldDelete)
        {
            token = "";
            shouldDelete = false;

            App.Log("Sending GET request...");
            var request =
                new ClientRequest()
                {
                    version = 1,
                    verb = "GET",
                    headers = new Dictionary<string, string>() { { "pwd", pwd } }
                };
            if (ServerStream == null)
                return false;
            SecureNet.SendObject(ServerStream, request);

            App.Log("Receiving GET response...");
            if (ServerStream == null)
                return false;
            using (var response = SecureNet.ReadObject<ServerResponse>(ServerStream))
            {
                App.Log($"Server Response: {response.ResponseSummary}");
                if (response.statusCode / 100 != 2)
                    throw response.CreateException();

                msg? m = JsonConvert.DeserializeObject<msg>(response.headers["msg"]);
                string status = m == null ? "(null)" : (m.from + ": " + m.subject);
                App.Log($"Message: {status}");
                if (m == null)
                    return false;
                else
                    token = m.token;

                string temp_file_path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
                try
                {
                    if (!App.ConfirmDownload(m.from, m.subject, m.body, out shouldDelete))
                        return false;
 
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
                    int file_count = 0;
                    long total_size_bytes = 0;
                    string manifest = ManifestZip(temp_file_path, pwd, out file_count, out total_size_bytes);
                    if (App.Cancelled)
                        return false;

                    string extraction_dir_path = "";
                    if (!App.ConfirmExtraction(manifest, file_count, total_size_bytes, out shouldDelete, out extraction_dir_path))
                        return false;

                    App.Log($"Saving downloaded files...");
                    ExtractZip(temp_file_path, pwd, extraction_dir_path);
                    return true;
                }
                finally
                {
                    if (File.Exists(temp_file_path))
                        File.Delete(temp_file_path);
                }
            }
        }

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

        private string ManifestZip(string zipFilePath, string pwd, out int fileCount, out long totalByteCount)
        {
            fileCount = 0;
            totalByteCount = 0;
            StringBuilder sb = new StringBuilder();
            Dictionary<string, int> ext_counts = new Dictionary<string, int>();
            using (var zip_file = new Ionic.Zip.ZipFile(zipFilePath))
            {
                zip_file.Password = pwd;
                foreach (var zip_entry in zip_file.Entries)
                {
                    if (zip_entry.IsDirectory)
                        continue;

                    string size_str = Utils.ByteCountToStr(zip_entry.UncompressedSize);
                    sb.AppendLine($"{zip_entry.FileName} ({size_str})");

                    string ext = Path.GetExtension(zip_entry.FileName);
                    if (!ext_counts.ContainsKey(ext))
                        ext_counts[ext] = 0;
                    ++ext_counts[ext];

                    ++fileCount;
                    totalByteCount += zip_entry.UncompressedSize;
                }
            }

            string ext_summary =
                "File Types:\r\n" +
                string.Join
                (
                    "\r\n", 
                    ext_counts
                        .Select(kvp => $"{kvp.Key}: {kvp.Value}")
                        .OrderBy(str => str)
                );

            return ext_summary + "\r\n\r\n" + sb.ToString();
        }

        private void ExtractZip(string zipFilePath, string pwd, string extractionDirPath)
        {
            using (var zip = new ZipFile(zipFilePath))
            {
                zip.Password = pwd;
                zip.ExtractProgress += Zip_ExtractProgress;
                zip.ExtractAll(extractionDirPath);
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
                    (double)e.EntriesSaved / Math.Max(e.EntriesTotal, 1),
                    (double)e.BytesTransferred / Math.Max(e.TotalBytesToTransfer, 1)
                );
            App.Progress(min_progress);
        }

        private void Zip_ExtractProgress(object? sender, ExtractProgressEventArgs e)
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
                    (double)e.EntriesExtracted / Math.Max(e.EntriesTotal, 1),
                    (double)e.BytesTransferred / Math.Max(e.TotalBytesToTransfer, 1)
                );
            App.Progress(min_progress);
        }

        private string m_lastZipCurrentFilename = "";
    }
}
