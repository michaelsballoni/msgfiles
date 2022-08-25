namespace msgfiles
{
    /// <summary>
    /// ServerApp implements to server functionality by
    /// satisfying the requirements of IServerApp for logging,
    /// sessions, email sending, and request processing
    /// It dynamically reloads settings and allow/block lists
    /// It performs maintenance to prune sessions, messages,
    /// files, and logs
    /// </summary>
    internal class ServerApp : IServerApp, IDisposable
    {
        public ServerApp()
        {
            string settings_file_path = Path.Combine(AppDocsDirPath, "settings.ini");
            if (!File.Exists(settings_file_path))
                throw new InputException($"settings.ini file does not exist in {AppDocsDirPath}");
            
            m_settings = new Settings(settings_file_path);

            if (!int.TryParse
            (
                m_settings.Get("application", "MaxSendPayloadMB"), 
                out MsgRequestHandler.MaxSendPayloadMB
            ))
            {
                throw new InputException("Invalid setting: MaxSendPayloadMB");
            }

            if (!int.TryParse
            (
                m_settings.Get("application", "ReceiveTimeoutSeconds"),
                out Server.ReceiveTimeoutSeconds
            ))
            {
                throw new InputException("Invalid setting: ReceiveTimeoutSeconds");
            }

            if (!int.TryParse
            (
                m_settings.Get("application", "ServerPort"),
                out ServerPort
            ))
            {
                throw new InputException("Invalid setting: ServerPort");
            }

            m_settingsWatcher = new FileSystemWatcher(AppDocsDirPath, "*.ini");
            m_settingsWatcher.Changed += SettingsWatcher_Changed;
            m_settingsWatcher.Created += SettingsWatcher_Changed;
            m_settingsWatcher.Deleted += SettingsWatcher_Changed;
            SettingsWatcher_Changed(new object(), new FileSystemEventArgs(WatcherChangeTypes.All, "", null));

            m_txtFilesWatcher = new FileSystemWatcher(AppDocsDirPath, "*.txt");
            m_txtFilesWatcher.Changed += TextWatcher_Changed;
            m_txtFilesWatcher.Created += TextWatcher_Changed;
            m_txtFilesWatcher.Deleted += TextWatcher_Changed;
            TextWatcher_Changed(new object(), new FileSystemEventArgs(WatcherChangeTypes.All, "", null));

            m_sessions = new SessionStore(Path.Combine(AppDocsDirPath, "sessions.db"));

            m_messageStore = new MessageStore(Path.Combine(AppDocsDirPath, "messages.db"));

            m_fileStore = new FileStore(m_settings.Get("application", "FileStoreDir"));

            m_logStore = new LogStore(Path.Combine(AppDocsDirPath, "logs"));

            string mail_server = m_settings.Get("application", "MailServer");
            if (string.IsNullOrWhiteSpace(mail_server))
                throw new InputException("Invalid setting: MailServer");

            int mail_port;
            if (!int.TryParse
            (
                m_settings.Get("application", "MailPort"),
                out mail_port
            ))
            {
                throw new InputException("Invalid setting: MailPort");
            }

            m_emailClient =
                new EmailClient
                (
                    mail_server,
                    mail_port,
                    m_settings.Get("application", "MailUsername"),
                    m_settings.Get("application", "MailPassword")
                );

            m_maintenanceTimer = new Timer(MaintenanceTimer, null, 0, 60 * 1000);

            var to_kvp = Utils.ParseEmail(m_settings.Get("application", "MailAdminAddress"));
            var to_dict = new Dictionary<string, string>();
            to_dict.Add(to_kvp.Key, to_kvp.Value);
            m_emailClient.SendEmail
            (
                m_settings.Get("application", "MailFromAddress"),
                to_dict,
                "Server Started Up",
                "So far so good..."
            );
        }

        public void Dispose()
        {
            m_sessions.Dispose();
            m_messageStore.Dispose();
            m_logStore.Dispose();
        }

        /// <summary>
        /// What directory contains the server's various setting files?
        /// </summary>
        public static string AppDocsDirPath
        {
            get
            {
                string path =
                Path.Combine
                    (
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "msgfiles-server"
                    );
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
        }

        public int ServerPort;
        public string FileStoreDirPath => 
            m_fileStore == null ? "" : m_fileStore.DirPath;

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

        /// <summary>
        /// Hand out a new request handler, supplying what it needs from this
        /// </summary>
        public IServerRequestHandler RequestHandler =>
            new MsgRequestHandler(m_allowBlock, m_fileStore, m_messageStore);

        public void Log(string msg)
        {
            m_logStore.Log(DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss") + " " + msg);
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

        /// <summary>
        /// Send the user the challenge token to verify they have access 
        /// to the email
        /// </summary>
        public void SendChallengeToken(string email, string display, string token)
        {
            m_allowBlock.EnsureEmailAllowed(email);

            m_emailClient.SendEmail
            (
                m_settings.Get("application", "MailFromAddress"),
                new Dictionary<string, string>() { { email , display } },
                "Message Files - Login Challenge",
                $"Copy and paste this token into your msgfiles application:\r\n\r\n" +
                $"{token}\r\n\r\n" +
                $"Questions or comments?  Feel free to reply to this message!"
            );
        }

        public void SendDeliveryMessage(string from, string toos, string message, string token)
        {
            string email_message =
                $"msgfiles from {from}:\r\n\r\n" +
                $"{message}\r\n\r\n" +
                $"Run the msgfiles application and paste this access token there:\r\n\r\n" +
                $"{token}\r\n\r\n" +
                $"Questions or comments?  Feel free to reply to this message!";
            SendEmail(from, toos, email_message);
        }

        /// <summary>
        /// Core email sending function
        /// </summary>
        /// <param name="from">Full email address to say the message is from</param>
        /// <param name="toos">List of full email addresses to send to</param>
        /// <param name="message">Body of the message to send</param>
        public void SendEmail(string from, string toos, string message)
        {
            var from_kvp = Utils.ParseEmail(from);
            m_allowBlock.EnsureEmailAllowed(from_kvp.Key);

            var toos_dict = new Dictionary<string, string>();
            foreach
            (
                var to_kvp in 
                    toos
                    .Split(';')
                    .Select(t => t.Trim())
                    .Where(t => t.Length > 0)
                    .Select(to => Utils.ParseEmail(to))
            )
            {
                m_allowBlock.EnsureEmailAllowed(to_kvp.Key);
                toos_dict.Add(to_kvp.Key, to_kvp.Value);
            }

            m_emailClient.SendEmail
            (
                m_settings.Get("application", "MailFromAddress"),
                toos_dict,
                $"msgfiles - New Message From {from}",
                message
            );
        }

        /// <summary>
        /// Load a text file's lines into a lookup list
        /// </summary>
        private HashSet<string> LoadFileList(string fileName)
        {
            string file_path = Path.Combine(AppDocsDirPath, fileName);
            if (File.Exists(file_path))
            {
                return
                    new HashSet<string>
                    (
                        File.ReadAllLines(file_path)
                        .Select(e => e.Trim().ToLower())
                        .Where(e => e.Length > 0 && e[0] != '#')
                    );
            }
            else
                return new HashSet<string>();
        }

        /// <summary>
        /// Prune sessions, messages, files, and logs
        /// </summary>
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

        private FileSystemWatcher m_settingsWatcher;
        private FileSystemWatcher m_txtFilesWatcher;

        private EmailClient m_emailClient;

        private FileStore m_fileStore;
        private MessageStore m_messageStore;
        private LogStore m_logStore;

        private Timer m_maintenanceTimer;
    }
}
