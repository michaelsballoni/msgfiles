﻿namespace msgfiles
{
    public class Session
    {
        public string token { get; set; } = "";
        public string email { get; set; } = "";
        public string display { get; set; } = "";
    }

    public class HandlerContext
    {
        public HandlerContext(Stream connectionStream)
        {
            ConnectionStream = connectionStream;
        }

        public Stream ConnectionStream;

        public static readonly ServerResponse StandardResponse =
            new ServerResponse()
            {
                version = 1,
                statusCode = 200,
                statusMessage = "OK",
                headers = new Dictionary<string, string>()
            };
    }

    public interface IServerRequestHandler
    {
        Task<ServerResponse> HandleRequestAsync(ClientRequest request, HandlerContext ctxt);
    }

    public interface IServerApp
    {
        // Define how requests are handled
        IServerRequestHandler RequestHandler { get; }

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
