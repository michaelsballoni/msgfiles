namespace msgfiles
{
    public class ClientMessage
    {
        public ClientMessage(List<string> to, string message, List<string> filePaths)
        {
            To = to;
            Message = message;
            Paths = filePaths;
        }

        public List<string> To;
        public string Message;
        public List<string> Paths;
    }
}
