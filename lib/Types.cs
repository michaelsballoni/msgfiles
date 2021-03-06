using Newtonsoft.Json;

namespace msgfiles
{
    public class NetworkException : Exception
    {
        public NetworkException(string msg) : base(msg) { }
    }

    public class InputException : Exception
    {
        public InputException(string msg) : base(msg) { }
    }

    public class ServerException : Exception
    {
        public ServerException(string msg) : base(msg) { }
    }

    public class Headered
    {
        public int version { get; set; }
        public Dictionary<string, string> headers { get; set; } = new Dictionary<string, string>();
    }

    public class ClientRequest : Headered
    {
        public string verb { get; set; } = "";
    }

    public class ServerResponse : Headered
    {
        public int statusCode { get; set; }
        public string statusMessage { get; set; } = "";

        [JsonIgnore]
        public int BaseCode => statusCode / 100;

        [JsonIgnore]
        public string ResponseSummary => $"{statusMessage} ({statusCode})";

        public Exception CreateException()
        {
            return new ServerException(ResponseSummary);
        }
    }
}
