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

    public class Headered
    {
        public int version { get; set; }
        public Dictionary<string, string> headers { get; set; } = new Dictionary<string, string>();
        public long contentLength { get; set; }
    }

    public class ClientRequest : Headered
    {
        public string verb { get; set; } = "";
    }

    public class ServerResponse : Headered, IDisposable
    {
        public void Dispose()
        {
            if (streamToSend != null)
            {
                streamToSend.Dispose();
                streamToSend = null;
            }
        }

        public int statusCode { get; set; }
        public string statusMessage { get; set; } = "";

        [JsonIgnore]
        public Stream? streamToSend { get; set; }

        [JsonIgnore]
        public string ResponseSummary => $"{statusCode} {statusMessage}";

        public Exception CreateException()
        {
            return new InputException(ResponseSummary);
        }
    }
}
