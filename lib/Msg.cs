namespace msgfiles
{
    public class Msg
    {
        public Msg(List<string> to, string subject, string body, List<string> filePaths)
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
}
