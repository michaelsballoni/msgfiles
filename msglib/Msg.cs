namespace msgfiles
{
    public class ClientMessage
    {
        public ClientMessage(List<string> to, string subject, string body, List<string> filePaths)
        {
            To = to;
            Subject = subject;
            Body = body;
            Paths = filePaths;
        }

        public List<string> To;
        public string Subject;
        public string Body;
        public List<string> Paths;
    }

    public class ServerMessage
    {
        public string token { get; set; } = "";
        public string from { get; set; } = "";
        public string to { get; set; } = "";
        public string subject { get; set; } = "";
        public string body { get; set; } = "";
        public string manifest { get; set; } = "";
        public DateTimeOffset created { get; set; }
    }
}
