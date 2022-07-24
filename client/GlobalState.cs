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

        public static string? Server;
        public static int Port;

        public static string? DisplayName;
        public static string? Email;
        public static string? SessionToken;

        public static Settings? Settings;
    }
}
