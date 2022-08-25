namespace msgfiles
{
    /// <summary>
    /// Basic files-in-a-directory storage manager
    /// </summary>
    public class FileStore
    {
        public FileStore(string dirPath)
        {
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            DirPath = dirPath;
        }

        public string DirPath { get; private set; }

        /// <summary>
        /// Add a file to the storage system
        /// </summary>
        public string StoreFile(string filePath)
        {
            string new_path = Path.Combine(DirPath, $"{Guid.NewGuid()}.dat");
            File.Copy(filePath, new_path);
            return new_path;
        }

        /// <summary>
        /// Prune old files
        /// Use seconds for unit tests, 86,400 for days
        /// </summary>
        public int DeleteOldFiles(int maxAgeSeconds)
        {
            int files_deleted = 0;

            DateTime oldest_date = DateTime.UtcNow - new TimeSpan(0, 0, maxAgeSeconds);

            var file_paths_to_delete = new List<string>();
            foreach (var file_path in Directory.GetFiles(DirPath))
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
    }
}
