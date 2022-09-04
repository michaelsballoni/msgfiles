namespace msgfiles
{
    /// <summary>
    /// HandlerContext provides request handlers 
    /// with what they need to do their thing
    /// </summary>
    public class HandlerContext
    {
        public HandlerContext
        (
            IServerApp app, 
            string clientAddress, 
            Dictionary<string, string> auth, 
            Stream connectionStream
        )
        {
            App = app;
            ClientAddress = clientAddress;
            Auth = auth;
            ConnectionStream = connectionStream;
        }

        public IServerApp App { get; private set; }
        public string ClientAddress { get; private set; }
        public Dictionary<string, string> Auth { get; private set; }
        public Stream ConnectionStream { get; private set; }
    }

    /// <summary>
    /// Request handlers, given context, handle requests
    /// </summary>
    public interface IServerRequestHandler
    {
        Task<ServerResponse> HandleRequestAsync(ClientRequest request, HandlerContext ctxt);
    }

    /// <summary>
    /// Servers must do the heavy lifting
    /// </summary>
    public interface IServerApp
    {
        // Define how client requests are handled
        IServerRequestHandler RequestHandler { get; }

        void Log(string msg);
        void LogRequest(string clientIp, string clientEmail, string verb, string token);

        // Send a challenge token to validate
        void SendChallengeToken(string email, string display, string token);

        // Send the message with the manifest and password
        void SendDeliveryMessage(string from, string to, string message, string pwd);

        // Sessions
        Session? GetSession(Dictionary<string, string> auth);
        Session CreateSession(Dictionary<string, string> auth);
        bool DropSession(Dictionary<string, string> auth);
    }
}
