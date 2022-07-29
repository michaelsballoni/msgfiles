using System.Diagnostics;
using System.IO;

namespace msgfiles
{
    internal class ServerApp : IServerApp
    {
        public ServerApp()
        {
            m_thisExeDirPath = 
                Path.GetDirectoryName(Process.GetCurrentProcess().Modules[0].FileName) 
                ?? 
                Environment.CurrentDirectory;

            m_settings = new Settings(Path.Combine(m_thisExeDirPath, "settings.ini"));

            LoadSettings();

            m_settingsWatcher = new FileSystemWatcher();
            m_settingsWatcher.Changed += SettingsWatcher_Changed;
            m_settingsWatcher.Created += SettingsWatcher_Changed;
            m_settingsWatcher.Deleted += SettingsWatcher_Changed;
        }

        private void SettingsWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                m_rwSettingsLock.EnterWriteLock();
                
                m_settings.Load();
                
                m_allowBlock.SetLists
                (
                    LoadFileList("allowed.txt"), 
                    LoadFileList("blocked.txt")
                );
            }
            finally
            {
                m_rwSettingsLock.ExitWriteLock();
            }
        }

        public Session CreateSession(Dictionary<string, string> auth)
        {
            m_allowBlock.EnsureEmailAllowed(auth["email"]);

            string session_key = Utils.GenToken();
            var new_session =
                new Session()
                {
                    token = session_key,
                    display = auth["display"],
                    email = auth["email"]
                };
            lock (m_sessions)
                m_sessions[session_key] = new_session;
            return new_session;
        }

        public Session? GetSession(Dictionary<string, string> auth)
        {
            Session? session;
            lock (m_sessions)
            {
                if (m_sessions.TryGetValue(auth["session"], out session) && session != null)
                    return session;
                else
                    return null;
            }
        }

        public bool DropSession(Dictionary<string, string> auth)
        {
            if (!auth.ContainsKey("session") || string.IsNullOrWhiteSpace(auth["session"]))
                return false;

            lock (m_sessions)
                return m_sessions.Remove(auth["session"]);
        }

        public void Log(string msg)
        {
            Console.WriteLine(msg);
        }

        public void SendChallengeToken(string email, string token)
        {
            m_allowBlock.EnsureEmailAllowed(email);

            // FORNOW - Just show the poor developer the token
            Console.WriteLine("Token for " + email);
            Console.WriteLine(token);
        }

        private HashSet<string> LoadFileList(string fileName)
        {
            string file_path = Path.Combine(m_thisExeDirPath, fileName);
            if (File.Exists(file_path))
                return new HashSet<string>(File.ReadAllLines(file_path));
            else
                return new HashSet<string>();
        }

        // FORNOW - Sessions should persist
        private Dictionary<string, Session> m_sessions = new Dictionary<string, Session>();

        private Settings m_settings;
        private AllowBlock m_allowBlock = new AllowBlock();

        private string m_thisExeDirPath;
        private FileSystemWatcher m_settingsWatcher;
        private ReaderWriterLockSlim m_rwSettingsLock = new ReaderWriterLockSlim();
    }
}
