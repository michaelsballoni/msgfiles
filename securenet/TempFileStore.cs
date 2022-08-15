namespace msgfiles
{
    public class TempFileUse : IDisposable
    {
        public TempFileUse(string extension)
        {
            FilePath = TempFileStore.GetPathToUse(extension);
        }

        public void Dispose()
        {
            Clear();
        }

        public string FilePath;

        public void Clear()
        {
            TempFileStore.RelinquishPath(FilePath);
            FilePath = "";
        }
    }

    public static class TempFileStore
    {
        public static string GetPathToUse(string extension)
        {
            lock (sm_tempFileDirPath)
            {
                if (!Directory.Exists(sm_tempFileDirPath))
                    Directory.CreateDirectory(sm_tempFileDirPath);
            }

            string file_path = 
                Path.Combine(sm_tempFileDirPath, $"{Guid.NewGuid()}{extension}");
            lock (sm_filesInUse)
                sm_filesInUse.Add(file_path);

            return file_path;
        }

        public static void RelinquishPath(string filePath)
        {
            if (filePath == "")
                return;

            lock (sm_filesInUse)
                sm_filesInUse.Remove(filePath);
        }

        public static void CleanupDir(int maxAgeSeconds)
        {
            lock (sm_tempFileDirPath)
            {
                if (!Directory.Exists(sm_tempFileDirPath))
                    return;
            }

            DateTime old_time = DateTime.UtcNow - new TimeSpan(0, 0, maxAgeSeconds);

            var file_paths_to_delete = new List<string>();
            var file_paths = Directory.GetFiles(sm_tempFileDirPath);
            lock (sm_filesInUse)
            {
                foreach (string file_path in file_paths)
                {
                    if (sm_filesInUse.Contains(file_path))
                        continue;

                    if (!File.Exists(file_path) || File.GetLastAccessTimeUtc(file_path) < old_time)
                        file_paths_to_delete.Add(file_path);
                }
            }

            foreach (var file_path in file_paths_to_delete)
            {
                try
                {
                    if (File.Exists(file_path))
                        File.Delete(file_path);
                }
                catch { }
            }
        }

        private static string sm_tempFileDirPath = 
            Path.Combine(Path.GetTempPath(), "msgfiles-temp");
        private static HashSet<string> sm_filesInUse = new HashSet<string>();
    }
}
