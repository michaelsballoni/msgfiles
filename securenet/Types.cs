using Newtonsoft.Json;

namespace msgfiles
{
    /// <summary>
    /// Did something bad happen with networking, worth trying again?
    /// </summary>
    public class NetworkException : Exception
    {
        public NetworkException(string msg) : base(msg) { }
    }

    /// <summary>
    /// Is there something wrong with the request such that retry won't help?
    /// </summary>
    public class InputException : Exception
    {
        public InputException(string msg) : base(msg) { }
    }

    /// <summary>
    /// Sort of an HTTP header
    /// </summary>
    public abstract class Headered
    {
        public int version { get; set; }
        public Dictionary<string, string> headers { get; set; } = new Dictionary<string, string>();
        public long contentLength { get; set; }
    }

    /// <summary>
    /// A request is headered, and adds the verb
    /// </summary>
    public class ClientRequest : Headered
    {
        public string verb { get; set; } = "";
    }

    /// <summary>
    /// A response is headered, and adds status code and message, and a stream to send
    /// </summary>
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
