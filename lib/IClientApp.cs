namespace msgfiles
{
    public interface IClientApp
    {
        bool Cancelled { get; }
        void Log(string msg);
    }
}
