using System.Diagnostics;

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

            m_settingsWatcher = new FileSystemWatcher(m_thisExeDirPath, "*.ini");
            m_settingsWatcher.Changed += SettingsWatcher_Changed;
            m_settingsWatcher.Created += SettingsWatcher_Changed;
            m_settingsWatcher.Deleted += SettingsWatcher_Changed;
            SettingsWatcher_Changed(new object(), new FileSystemEventArgs(WatcherChangeTypes.All, "", null));

            m_txtFilesWatcher = new FileSystemWatcher(m_thisExeDirPath, "*.txt");
            m_txtFilesWatcher.Changed += TextWatcher_Changed;
            m_txtFilesWatcher.Created += TextWatcher_Changed;
            m_txtFilesWatcher.Deleted += TextWatcher_Changed;
            TextWatcher_Changed(new object(), new FileSystemEventArgs(WatcherChangeTypes.All, "", null));

            m_sessions = new SessionDb(Path.Combine(m_thisExeDirPath, "sessions.db"));

            m_emailClient =
                new EmailClient
                (
                    m_settings.Get("application", "MailUsername"),
                    m_settings.Get("application", "MailPassword"),
                    m_settings.Get("application", "MailRegion")
                );
            m_emailClient.SendEmailAsync
            (
                m_settings.Get("application", "MailFromAddress"), 
                new Dictionary<string, string>() { { m_settings.Get("application", "MailAdminAddress"), "Message Files Hello"} },
                "Server Started Up", 
                "So far so good..."
            ).Wait();
        }

        private void SettingsWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            m_settings.Load();
        }

        private void TextWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            m_allowBlock.SetLists
            (
                LoadFileList("allowed.txt"),
                LoadFileList("blocked.txt")
            );
        }

        public Session CreateSession(Dictionary<string, string> auth)
        {
            m_allowBlock.EnsureEmailAllowed(auth["email"]);
            return m_sessions.CreateSession(auth["email"], auth["display"]);
        }

        public Session? GetSession(Dictionary<string, string> auth)
        {
            return m_sessions.GetSession(auth["session"]);
        }

        public bool DropSession(Dictionary<string, string> auth)
        {
            return m_sessions.DropSession(auth["session"]);
        }

        public void Log(string msg)
        {
            Console.WriteLine(msg);
        }

        public async Task SendChallengeTokenAsync(string email, string display, string token)
        {
            m_allowBlock.EnsureEmailAllowed(email);

            await m_emailClient.SendEmailAsync
            (
                m_settings.Get("application", "MailFromAddress"),
                new Dictionary<string, string>() { { email , display } },
                "Message Files - Login Challenge",
                $"Copy and paste this token into the msgfiles application:\r\n\r\n" +
                $"{token}\r\n\r\n" +
                $"Questions or comments?  Feel free to reply to this message!"
            );

            Console.WriteLine("Token for " + email);
            Console.WriteLine(token);
        }

        public IServerRequestHandler RequestHandler => new MsgRequestHandler();

        public async Task SendMessage(string from, string toos, string message)
        {
            var from_kvp = Utils.ParseEmail(from);
            m_allowBlock.EnsureEmailAllowed(from_kvp.Key);

            var toos_dict = new Dictionary<string, string>();
            foreach (string to in toos.Trim().Trim(';').Split(';'))
            {
                var to_kvp = Utils.ParseEmail(to);
                m_allowBlock.EnsureEmailAllowed(to_kvp.Key);
                toos_dict.Add(to_kvp.Key, to_kvp.Value);
            }

            await m_emailClient.SendEmailAsync
            (
                m_settings.Get("application", "MailFromAddress"),
                toos_dict,
                $"Message Files - New Message From {from}",
                message
            );
        }

        private HashSet<string> LoadFileList(string fileName)
        {
            string file_path = Path.Combine(m_thisExeDirPath, fileName);
            if (File.Exists(file_path))
            {
                return
                    new HashSet<string>
                    (
                        File.ReadAllLines(file_path)
                        .Select(e => e.Trim().ToLower())
                        .Where(e => e.Length > 0)
                    );
            }
            else
                return new HashSet<string>();
        }

        private SessionDb m_sessions;

        private Settings m_settings;
        private AllowBlock m_allowBlock = new AllowBlock();

        private string m_thisExeDirPath;
        private FileSystemWatcher m_settingsWatcher;
        private FileSystemWatcher m_txtFilesWatcher;

        private EmailClient m_emailClient;
    }
}

