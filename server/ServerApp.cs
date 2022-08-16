using System.Diagnostics;
using System.Threading;

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

            if (!int.TryParse
            (
                m_settings.Get("application", "MaxSendPayloadMB"), 
                out MsgRequestHandler.MaxSendPayloadMB
            ))
            {
                throw new InputException("Invalid settings.ini: MaxSendPayloadMB");
            }

            if (!int.TryParse
            (
                m_settings.Get("application", "ReceiveTimeoutSeconds"),
                out Server.ReceiveTimeoutSeconds
            ))
            {
                throw new InputException("Invalid settings.ini: ReceiveTimeoutSeconds");
            }

            if (!int.TryParse
            (
                m_settings.Get("application", "ServerPort"),
                out ServerPort
            ))
            {
                throw new InputException("Invalid settings.ini: ServerPort");
            }

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

            m_sessions = new SessionStore(Path.Combine(m_thisExeDirPath, "sessions.db"));

            m_messageStore = new MessageStore(Path.Combine(m_thisExeDirPath, "messages.db"));

            m_fileStore = new FileStore(m_settings.Get("application", "FileStoreDir"));

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

        public int ServerPort;

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

        public IServerRequestHandler RequestHandler =>
            new MsgRequestHandler(m_allowBlock, m_fileStore, m_messageStore);

        public void Log(string msg)
        {
            Console.WriteLine(msg);
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

        public async Task SendMailDeliveryMessageAsync(string from, string toos, string message, string pwd)
        {
            string email_message =
                $"msgfiles from {from}:\r\n\r\n" +
                $"{message}\r\n\r\n" +
                $"Run the msgfiles application and paste this access token there:\r\n\r\n" +
                $"{pwd}\r\n\r\n" +
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

        private void MaintenanceTimer(object state)
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
            }
        }

        private SessionStore m_sessions;

        private Settings m_settings;
        private AllowBlock m_allowBlock = new AllowBlock();

        private string m_thisExeDirPath;
        private FileSystemWatcher m_settingsWatcher;
        private FileSystemWatcher m_txtFilesWatcher;

        private EmailClient m_emailClient;

        private FileStore m_fileStore;
        private MessageStore m_messageStore;

        private Timer m_maintenanceTimer;
    }
}
