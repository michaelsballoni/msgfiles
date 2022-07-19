﻿namespace msgfiles
{
    public class Session
    {
        public string token { get; set; }
        public string email { get; set; }
        public string display { get; set; }
    }

    public interface IApp
    {
        // Write to a log file and/or console
        void Log(string message);

        // Send the user a token to validate with
        void SendToken(string email, string token);

        // Work with sessions
        Session GetSession(Dictionary<string, string> auth);
        Session CreateSession(Dictionary<string, string> auth);

        /* FORNOW
        void StoreMessage(Message msg);
        List<Message> GetMessages(string email);
        Message GetMessage(string token);
        */
    }
}
