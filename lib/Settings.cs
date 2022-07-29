using System.Text;

namespace msgfiles
{
    public class Settings
    {
        public Settings(string filePath)
        {
            m_filePath = filePath;
            m_settings = new Dictionary<string, Dictionary<string, string>>();

            if (File.Exists(m_filePath))
                Load();
        }

        public void Load()
        {
            try
            {
                m_rwLock.EnterWriteLock();

                m_settings.Clear();

                Dictionary<string, string>? cur_dict = null;
                string[] lines = File.ReadAllLines(m_filePath, Encoding.UTF8);
                for (int n = 0; n < lines.Length; ++n)
                {
                    string line = lines[n].Trim();
                    if (line.Length == 0 || line[0] == '#')
                        continue;

                    if (line[0] == '[')
                    {
                        string section_name = line.Trim('[', ']');
                        if (m_settings.ContainsKey(section_name))
                            throw new Exception("Duplicate section: " + (n + 1));

                        cur_dict = new Dictionary<string, string>();
                        m_settings[section_name] = cur_dict;
                    }
                    else
                    {
                        int equals = line.IndexOf('=');
                        if (equals <= 0)
                            throw new Exception("Invalid line: " + (n + 1));

                        string setting_name = line.Substring(0, equals).Trim();
                        string setting_value = line.Substring(equals + 1).Trim().Trim('"');

                        if (cur_dict == null)
                            throw new Exception("No section: " + (n + 1));

                        if (cur_dict.ContainsKey(setting_name))
                            throw new Exception("Duplicate setting: " + (n + 1));

                        cur_dict.Add(setting_name, setting_value);
                    }
                }
            }
            finally
            {
                m_rwLock.ExitWriteLock();
            }
        }

        public void Save()
        {
            try
            {
                m_rwLock.EnterReadLock();

                StringBuilder sb = new StringBuilder();
                foreach (string section_name in m_settings.Keys)
                {
                    sb.AppendLine($"[{section_name}]");
                    Dictionary<string, string> settings = m_settings[section_name];
                    foreach (var kvp in settings)
                    {
                        sb.AppendLine($"{kvp.Key} = {kvp.Value}");
                    }
                    sb.AppendLine();
                }
                File.WriteAllText(m_filePath, sb.ToString());
            }
            finally
            {
                m_rwLock.ExitReadLock();
            }
        }

        public string Get(string section, string name)
        {
            try
            {
                m_rwLock.EnterReadLock();

                if (!m_settings.ContainsKey(section) || !m_settings[section].ContainsKey(name))
                    return "";
                else
                    return m_settings[section][name];
            }
            finally
            {
                m_rwLock.ExitReadLock();
            }
        }

        public void Set(string section, string name, string? settingValue)
        {
            try
            {
                m_rwLock.EnterWriteLock();

                if (!m_settings.ContainsKey(section))
                    m_settings[section] = new Dictionary<string, string>();

                if (settingValue == null)
                    m_settings[section].Remove(name);
                else
                    m_settings[section][name] = settingValue;
            }
            finally
            {
                m_rwLock.ExitWriteLock();
            }
        }

        public List<string> GetSeries(string section, string name)
        {
            try
            {
                m_rwLock.EnterReadLock();

                List<string> ret_val = new List<string>();
                if (!m_settings.ContainsKey(section))
                    return ret_val;

                var cur_dict = m_settings[section];
                for (int n = 1; ; ++n)
                {
                    string cur_key = name + n;
                    if (!cur_dict.ContainsKey(cur_key))
                        break;
                    else
                        ret_val.Add(cur_dict[cur_key]);
                }
                return ret_val;
            }
            finally
            {
                m_rwLock.ExitReadLock();
            }
        }

        public void SetSeries(string section, string name, List<string> seriesValues)
        {
            try
            {
                m_rwLock.EnterWriteLock();

                if (!m_settings.ContainsKey(section))
                    m_settings[section] = new Dictionary<string, string>();

                var cur_dict = m_settings[section];
                for (int n = 1; ; ++n)
                {
                    string cur_key = name + n;
                    if (!cur_dict.ContainsKey(cur_key))
                        break;
                    else
                        cur_dict.Remove(cur_key);
                }

                for (int v = 0; v < seriesValues.Count; ++v)
                    cur_dict[name + (v + 1)] = seriesValues[v];
            }
            finally
            {
                m_rwLock.ExitWriteLock();
            }
        }

        private string m_filePath;
        private Dictionary<string, Dictionary<string, string>> m_settings;
        private ReaderWriterLockSlim m_rwLock = new ReaderWriterLockSlim();
    }
}
