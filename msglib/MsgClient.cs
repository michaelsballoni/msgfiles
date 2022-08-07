using Ionic.Zip;

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
                        headers = new Dictionary<string, string>()
                        {
                            { "to", string.Join("; ", to) },
                            { "subject", subject },
                            { "body", body },
                            { "pwd", pwd },
                            { "packageSizeBytes", zip_file_size_bytes.ToString() },
                            { "packageHash", zip_hash }
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

        /* FORNOW - Finish this
        public List<msg> GetMessages()
        {
            var request =
                new ClientRequest()
                {
                    version = 1,
                    verb = "POP",
                    headers = new Dictionary<string, string>() { }
                };
            // FORNOW
        }

        public msg GetMessage(msg m)
        {
            
        }

        public void DeleteMessage(msg m)
        {
            
        }
        */

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
