namespace msgfiles
{
    public class LogStore : IDisposable
    {
        public LogStore(string dirPath, int logPathCheckSkipCount = 100)
        {
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            m_dirPath = dirPath;
            m_logPathCheckSkipCount = logPathCheckSkipCount;
        }

        public void Dispose()
        {
            if (m_curOpenFile != null)
            {
                m_curOpenFile.Dispose();
                m_curOpenFile = null;
            }
        }

        public void Log(string msg)
        {
            if
            (
                (
                    (++m_logPathCheckCounter % m_logPathCheckSkipCount) == 0 
                    && 
                    m_curOpenFilePath != LogPath
                ) 
                || 
                m_curOpenFile == null
            )
            {
                if (m_curOpenFile != null)
                {
                    m_curOpenFile.Dispose();
                    m_curOpenFile = null;
                }
                m_curOpenFilePath = LogPath;
                m_curOpenFile = new StreamWriter(m_curOpenFilePath, append: true);
                m_curOpenFile.AutoFlush = true;
            }

            m_curOpenFile.WriteLine(msg);
        }

        public int DeleteOldLogs(int maxAgeSeconds)
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

        private string LogPath =>
            Path.Combine
            (
                m_dirPath, 
                DateTime.UtcNow.ToString("yyyy-MM-dd") + ".log"
            );

        private string m_curOpenFilePath = "";
        private StreamWriter? m_curOpenFile;
        private long m_logPathCheckCounter = 0;
        private int m_logPathCheckSkipCount;

        private string m_dirPath;
    }
}
