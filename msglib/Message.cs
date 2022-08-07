namespace msgfiles
{
    public class msg
    {
        public string token { get; set; } = "";
        public string from { get; set; } = "";
        public string to { get; set; } = "";
        public string subject { get; set; } = "";
        public string body { get; set; } = "";
        public string manifest { get; set; } = "";
        public int fileCount { get; set; } = 0;
        public double fileSizeMB { get; set; } = 0.0;
        public string fileHash { get; set; } = "";
        public DateTimeOffset created { get; set; }
    }
}
