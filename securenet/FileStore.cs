using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace msgfiles
{
    public class FileStore
    {
        public FileStore(string dirPath)
        {
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            m_dirPath = dirPath;
        }

        public string DirPath => m_dirPath;

        public string StoreFile(string filePath)
        {
            string new_filename = $"{Guid.NewGuid()}.dat";
            string new_path = Path.Combine(m_dirPath, new_filename);
            File.Copy(filePath, new_path);
            return new_path;
        }

        public int DeleteOldFiles(int maxAgeSeconds)
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

        private string m_dirPath;
    }
}
