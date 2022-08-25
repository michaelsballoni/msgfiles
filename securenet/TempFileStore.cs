using System.Threading;

namespace msgfiles
{
    /// <summary>
    /// TempFileUse interacts with TempFileStore to define a period during which
    /// a temp file is in use and should not be deleted
    /// </summary>
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

    /// <summary>
    /// TempFileStore manages a temp directory of temp files that are in use
    /// At startup, and regularly thereafter, temp files are purged
    /// leaving the system clean
    /// </summary>
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

        public static void CleanupDir(object? state)
        {
            int max_seconds =
                (state == null || !(state is int))
                ? sm_cleanupIntervalSeconds
                : (int)state;

            if (sm_inCleanupTimer)
                return;
            lock (sm_cleanupTimerLock)
            {
                if (sm_inCleanupTimer)
                    return;
                sm_inCleanupTimer = true;

                try
                {
                    lock (sm_tempFileDirPath)
                    {
                        if (!Directory.Exists(sm_tempFileDirPath))
                            return;
                    }

                    DateTime old_time =
                        DateTime.UtcNow - new TimeSpan(0, 0, max_seconds);

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
                finally
                {
                    sm_inCleanupTimer = false;
                }
            }
        }

        /// <summary>
        /// Stop the cleanup timer
        /// Used by unit tests
        /// </summary>
        public static void DisableAutoCleanup()
        {
            sm_cleanupTimer.Dispose();
        }

        private static string sm_tempFileDirPath = 
            Path.Combine(Path.GetTempPath(), "msgfiles-temp");

        private static HashSet<string> sm_filesInUse = new HashSet<string>();

        private const int sm_cleanupIntervalSeconds = 10;
        private static Timer sm_cleanupTimer = 
            new Timer(CleanupDir, null, 0, sm_cleanupIntervalSeconds * 1000);
        private static object sm_cleanupTimerLock = new object();
        private static bool sm_inCleanupTimer = false;
    }
}
