using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

namespace msgfiles
{
    public class SettingsTests
    {
        [Test]
        public void TestSettings()
        {
            string settings_file_path = "UnitTestSettingsFile.ini";
            if (File.Exists(settings_file_path))
                File.Delete(settings_file_path);

            {
                var settings = new Settings(settings_file_path);
            }
            {
                var settings = new Settings(settings_file_path);
                settings.Save();
            }
            {
                var settings = new Settings(settings_file_path);
                settings.Set("section", "foo", "bar");
                Assert.AreEqual("bar", settings.Get("section", "foo"));
                settings.Save();
            }
            {
                var settings = new Settings(settings_file_path);
                Assert.AreEqual("bar", settings.Get("section", "foo"));
                
                settings.SetSeries("vals", "val", new List<string>() { "foo", "bar" });
                
                var series = settings.GetSeries("vals", "val");
                Assert.AreEqual(2, series.Count);
                Assert.AreEqual("foo", series[0]);
                Assert.AreEqual("bar", series[1]);

                settings.Save();
            }
            {
                var settings = new Settings(settings_file_path);

                var series = settings.GetSeries("vals", "val");
                Assert.AreEqual(2, series.Count);
                Assert.AreEqual("foo", series[0]);
                Assert.AreEqual("bar", series[1]);

                settings.SetSeries("vals", "val", new List<string>() { "blet" });

                settings.Save();
            }

            {
                var settings = new Settings(settings_file_path);

                var series = settings.GetSeries("vals", "val");
                Assert.AreEqual(1, series.Count);
                Assert.AreEqual("blet", series[0]);

                settings.Save();
            }
        }
    }
}
