namespace msgfiles
{
    public class Session
    {
        public string token { get; set; } = "";
        public string email { get; set; } = "";
        public string display { get; set; } = "";
    }

    public interface IServerApp
    {
        // Write to a log file and/or console
        void Log(string msg);

        // Send the user a challenge token to validate with
        Task SendChallengeTokenAsync(string email, string display, string token);

        // Send recipients the message
        Task SendMessage(string from, string toos, string message);

        // Work with sessions
        Session? GetSession(Dictionary<string, string> auth);
        Session CreateSession(Dictionary<string, string> auth);
        bool DropSession(Dictionary<string, string> auth);

        /* FORNOW - Message DB
        void StoreMessage(Message msg);
        List<Message> GetMessages(string email);
        Message GetMessage(string token);
        */
    }
}
