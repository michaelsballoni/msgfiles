namespace msgfiles
{
    public static class GlobalState
    {
        public static bool Init()
        {
            string docs_dir = 
                Path.Combine
                (
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                    "msgfiles"
                );
            if (!Directory.Exists(docs_dir))
                Directory.CreateDirectory(docs_dir);

            Settings = new Settings(Path.Combine(docs_dir, "settings.ini"));

            Server = Settings.Get("client", "server");
            if (!int.TryParse(Settings.Get("client", "port"), out Port))
                Port = 9914;

            DisplayName = Settings.Get("client", "display");
            Email = Settings.Get("client", "email");

            SessionToken = Utils.Decrypt(Settings.Get("client", "session"), Key);

            return Ready;
        }

        public static bool Ready
        {
            get
            {
                return
                    !string.IsNullOrEmpty(GlobalState.Server)
                    &&
                    !string.IsNullOrEmpty(GlobalState.DisplayName)
                    &&
                    !string.IsNullOrEmpty(GlobalState.Email)
                    &&
                    !string.IsNullOrEmpty(GlobalState.SessionToken)
                    ;
            }
        }

        public static void SaveSettings()
        {
            if (Settings == null)
                throw new Exception("Call Init() first");

            Settings.Set("client", "server", Server);
            Settings.Set("client", "port", Port.ToString());

            Settings.Set("client", "display", DisplayName);
            Settings.Set("client", "email", Email);

            Settings.Set("client", "session", Utils.Encrypt(SessionToken, Key));

            Settings.Save();
        }

        private static string Key
        {
            get
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.DriveType == DriveType.Fixed && drive.IsReady)
                    {
                        return
                            Utils.Hash256Str
                            (
                                drive.VolumeLabel + "|" +
                                drive.DriveFormat + "|" +
                                drive.RootDirectory + "|" +
                                ""
                            );
                    }
                }
                throw new Exception("Failed to compute security key");
            }
        }

        public static string Server = "";
        public static int Port = 9914;

        public static string DisplayName = "";
        public static string Email = "";
        public static string SessionToken = "";

        public static Settings? Settings;
    }
}
