using System.Diagnostics;
using System.Threading;

namespace msgfiles
{
    internal class ServerApp : IServerApp, IDisposable
    {
        public ServerApp()
        {
            m_docsDirPath =
                Path.Combine
                (
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "msgfiles-server"
                );
            if (!Directory.Exists(m_docsDirPath))
                Directory.CreateDirectory(m_docsDirPath);

            string settings_file_path = Path.Combine(m_docsDirPath, "settings.ini");
            if (!File.Exists(settings_file_path))
                throw new InputException($"Startup Error: settings.ini file does not exist in {m_docsDirPath}");
            
            m_settings = new Settings(settings_file_path);

            if (!int.TryParse
            (
                m_settings.Get("application", "MaxSendPayloadMB"), 
                out MsgRequestHandler.MaxSendPayloadMB
            ))
            {
                throw new InputException("Startup Error: Invalid settings.ini: MaxSendPayloadMB");
            }

            if (!int.TryParse
            (
                m_settings.Get("application", "ReceiveTimeoutSeconds"),
                out Server.ReceiveTimeoutSeconds
            ))
            {
                throw new InputException("Startup Error: Invalid settings.ini: ReceiveTimeoutSeconds");
            }

            if (!int.TryParse
            (
                m_settings.Get("application", "ServerPort"),
                out ServerPort
            ))
            {
                throw new InputException("Startup Error: Invalid settings.ini: ServerPort");
            }

            m_settingsWatcher = new FileSystemWatcher(m_docsDirPath, "*.ini");
            m_settingsWatcher.Changed += SettingsWatcher_Changed;
            m_settingsWatcher.Created += SettingsWatcher_Changed;
            m_settingsWatcher.Deleted += SettingsWatcher_Changed;
            SettingsWatcher_Changed(new object(), new FileSystemEventArgs(WatcherChangeTypes.All, "", null));

            m_txtFilesWatcher = new FileSystemWatcher(m_docsDirPath, "*.txt");
            m_txtFilesWatcher.Changed += TextWatcher_Changed;
            m_txtFilesWatcher.Created += TextWatcher_Changed;
            m_txtFilesWatcher.Deleted += TextWatcher_Changed;
            TextWatcher_Changed(new object(), new FileSystemEventArgs(WatcherChangeTypes.All, "", null));

            m_sessions = new SessionStore(Path.Combine(m_docsDirPath, "sessions.db"));

            m_messageStore = new MessageStore(Path.Combine(m_docsDirPath, "messages.db"));

            m_fileStore = new FileStore(m_settings.Get("application", "FileStoreDir"));

            m_logStore = new LogStore(Path.Combine(m_docsDirPath, "logs"));

            m_emailClient =
                new EmailClient
                (
                    m_settings.Get("application", "MailServer"),
                    int.Parse(m_settings.Get("application", "MailPort")),
                    m_settings.Get("application", "MailUsername"),
                    m_settings.Get("application", "MailPassword")
                );

            m_maintenanceTimer = new Timer(MaintenanceTimer, null, 0, 60 * 1000);

            var to_kvp = Utils.ParseEmail(m_settings.Get("application", "MailAdminAddress"));
            var to_dict = new Dictionary<string, string>();
            to_dict.Add(to_kvp.Key, to_kvp.Value);
            m_emailClient.SendEmailAsync
            (
                m_settings.Get("application", "MailFromAddress"),
                to_dict,
                "Server Started Up",
                "So far so good..."
            ).Wait();
        }

        public void Dispose()
        {
            m_sessions.Dispose();
            m_messageStore.Dispose();
            m_logStore.Dispose();
        }

        public int ServerPort;
        public string AppDocsDirPath => m_docsDirPath;
        public string FileStoreDirPath => m_fileStore == null ? "" : m_fileStore.DirPath;

        private void SettingsWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            m_settings.Load();
        }

        private void TextWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            m_allowBlock.SetLists
            (
                LoadFileList("allow.txt"),
                LoadFileList("block.txt")
            );
        }

        public IServerRequestHandler RequestHandler =>
            new MsgRequestHandler(m_allowBlock, m_fileStore, m_messageStore);

        public void Log(string msg)
        {
            m_logStore.Log(DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss") + " " +msg);
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

        public string StoreFile(string filePath)
        {
            return m_fileStore.StoreFile(filePath);
        }

        public async Task SendChallengeTokenAsync(string email, string display, string token)
        {
            m_allowBlock.EnsureEmailAllowed(email);

            await m_emailClient.SendEmailAsync
            (
                m_settings.Get("application", "MailFromAddress"),
                new Dictionary<string, string>() { { email , display } },
                "Message Files - Login Challenge",
                $"Copy and paste this token into your msgfiles application:\r\n\r\n" +
                $"{token}\r\n\r\n" +
                $"Questions or comments?  Feel free to reply to this message!"
            );

            Console.WriteLine("Token for " + email);
            Console.WriteLine(token);
        }

        public async Task SendMailDeliveryMessageAsync(string from, string toos, string message, string token)
        {
            string email_message =
                $"msgfiles from {from}:\r\n\r\n" +
                $"{message}\r\n\r\n" +
                $"Run the msgfiles application and paste this access token there:\r\n\r\n" +
                $"{token}\r\n\r\n" +
                $"Questions or comments?  Feel free to reply to this message!";
            await SendEmailAsync(from, toos, email_message).ConfigureAwait(false);
        }

        public async Task SendEmailAsync(string from, string toos, string message)
        {
            var from_kvp = Utils.ParseEmail(from);
            m_allowBlock.EnsureEmailAllowed(from_kvp.Key);

            var toos_dict = new Dictionary<string, string>();
            foreach (string to in toos.Split(';').Select(t => t.Trim()).Where(t => t.Length > 0))
            {
                var to_kvp = Utils.ParseEmail(to);
                m_allowBlock.EnsureEmailAllowed(to_kvp.Key);
                toos_dict.Add(to_kvp.Key, to_kvp.Value);
            }

            await m_emailClient.SendEmailAsync
            (
                m_settings.Get("application", "MailFromAddress"),
                toos_dict,
                $"msgfiles - New Message From {from}",
                message
            );
        }

        private HashSet<string> LoadFileList(string fileName)
        {
            string file_path = Path.Combine(m_docsDirPath, fileName);
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

        private void MaintenanceTimer(object? state)
        {
            int age_off_days;
            if (int.TryParse(
                m_settings.Get("application", "AgeOffDays"),
                out age_off_days
            ))
            {
                m_sessions.DropOldSessions(86400 * age_off_days);
                m_messageStore.DeleteOldMessages(86400 * age_off_days);
                m_fileStore.DeleteOldFiles(86400 * age_off_days);
                m_logStore.DeleteOldLogs(86400 * age_off_days);
            }
        }

        private SessionStore m_sessions;

        private Settings m_settings;
        private AllowBlock m_allowBlock = new AllowBlock();

        private string m_docsDirPath;
        private FileSystemWatcher m_settingsWatcher;
        private FileSystemWatcher m_txtFilesWatcher;

        private EmailClient m_emailClient;

        private FileStore m_fileStore;
        private MessageStore m_messageStore;
        private LogStore m_logStore;

        private Timer m_maintenanceTimer;
    }
}
