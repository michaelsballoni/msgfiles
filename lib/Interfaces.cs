namespace msgfiles
{
    public interface ILog
    {
        void Log(string message);
    }

    public interface ITokenSender
    {
        void SendToken(string token);
    }
}
