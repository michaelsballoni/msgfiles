namespace msgfiles
{
    /// <summary>
    /// Manage log file storage
    /// </summary>
    public class LogStore : IDisposable
    {
        public LogStore(string dirPath, string filename)
        {
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            m_dirPath = dirPath;
            m_filename = filename;
        }

        public void Dispose()
        {
            lock (m_outputLock)
            {
                if (m_curOpenFile != null)
                {
                    m_curOpenFile.Dispose();
                    m_curOpenFile = null;
                }
            }
        }

        /// <summary>
        /// Write a message to a log file, 
        /// ensuring the log file has the right timestamp
        /// </summary>
        public void Log(string msg)
        {
            lock (m_outputLock)
            {
                if (m_curOpenFileStamp != LogFileStamp)
                {
                    if (m_curOpenFile != null)
                    {
                        m_curOpenFile.Dispose();
                        m_curOpenFile = null;
                    }

                    m_curOpenFile = new StreamWriter(LogPath, append: true);
                    m_curOpenFile.AutoFlush = true;
                    m_curOpenFileStamp = LogFileStamp;
                }

                if (m_curOpenFile == null)
                    throw new NullReferenceException("m_curOpenFile");
                else
                    m_curOpenFile.WriteLine(msg);
            }
        }

        /// <summary>
        /// Prune the directory of log files
        /// </summary>
        public int DeleteOldLogs(int maxAgeSeconds) // in seconds for unit tests
        {
            int files_deleted = 0;

            DateTime oldest_date = DateTime.UtcNow - new TimeSpan(0, 0, maxAgeSeconds);

            var file_paths_to_delete = new List<string>();
            foreach (var file_path in Directory.GetFiles(m_dirPath))
            {
                try
                {
                    var file_info = new FileInfo(file_path);
                    if
                    (
                        file_info.CreationTimeUtc < oldest_date
                        &&
                        file_info.LastAccessTimeUtc < oldest_date
                    )
                    {
                        file_paths_to_delete.Add(file_path);
                    }
                }
                catch { }
            }

            foreach (string file_path in file_paths_to_delete)
            {
                try
                {
                    File.Delete(file_path);
                    ++files_deleted;
                }
                catch { }
            }

            return files_deleted;
        }

        private string LogPath => Path.Combine(m_dirPath, $"{m_filename}-{LogFileStamp}.log");
        private string LogFileStamp => DateTime.UtcNow.ToString("yyyy-MM-dd");

        private string m_curOpenFileStamp = "";
        private StreamWriter? m_curOpenFile;
        private object m_outputLock = new object();

        private string m_dirPath;
        private string m_filename;
    }
}
