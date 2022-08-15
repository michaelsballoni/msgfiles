using System.Threading;
using System.IO;

using NUnit.Framework;

namespace msgfiles
{
    public class TempFileStoreTests
    {
        [Test]
        public void TestTempFileStore()
        {
            var temp_file_use = new TempFileUse(".txt");
            string temp_file_path = temp_file_use.FilePath;
            File.WriteAllText(temp_file_path, "foo bar");
            
            Thread.Sleep(1200);
            TempFileStore.CleanupDir(1);
            Assert.AreEqual("foo bar", File.ReadAllText(temp_file_path));
            
            temp_file_use.Dispose();

            Thread.Sleep(1200);
            TempFileStore.CleanupDir(1);
            Assert.IsTrue(!File.Exists(temp_file_path));
        }
    }
}
