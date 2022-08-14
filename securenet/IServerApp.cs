namespace msgfiles
{
    public class Session
    {
        public string token { get; set; } = "";
        public string email { get; set; } = "";
        public string display { get; set; } = "";
        public Dictionary<string, string> variables { get; set; } = new Dictionary<string, string>();
    }

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

        public IServerApp App;
        public string ClientAddress;
        public Dictionary<string, string> Auth;
        public Stream ConnectionStream;

        public static readonly ServerResponse StandardResponse =
            new ServerResponse()
            {
                version = 1,
                statusCode = 200,
                statusMessage = "OK"
            };
    }

    public interface IServerRequestHandler
    {
        Task<ServerResponse> HandleRequestAsync(ClientRequest request, HandlerContext ctxt);
    }

    public interface IServerApp
    {
        // Define how client requests are handled
        IServerRequestHandler RequestHandler { get; }

        void Log(string msg);

        // Send a challenge token to validate
        Task SendChallengeTokenAsync(string email, string display, string token);

        // Send the message with the manifest and password
        Task SendMailDeliveryMessageAsync(string from, string toos, string message, string pwd);

        // Sessions
        Session? GetSession(Dictionary<string, string> auth);
        Session CreateSession(Dictionary<string, string> auth);
        bool DropSession(Dictionary<string, string> auth);
    }
}
