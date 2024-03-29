﻿using System.Net.NetworkInformation;

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

            try
            {
                SessionToken = Utils.Decrypt(Settings.Get("client", "session"), Key);
            }
            catch
            {
                SessionToken = "";
            }

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
                return Utils.HashString(MacAddress + " - " + VolumeLabel);
            }
        }

        private static string MacAddress
        {
            get
            {
                foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (adapter.OperationalStatus == OperationalStatus.Up)
                        return adapter.GetPhysicalAddress().ToString();
                }
                return "";
            }
        }

        private static string VolumeLabel
        {
            get
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.DriveType == DriveType.Fixed && drive.IsReady)
                    {
                        return
                            drive.VolumeLabel + "|" + 
                            drive.DriveFormat + "|" + 
                            drive.RootDirectory;
                    }
                }
                return "";
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
