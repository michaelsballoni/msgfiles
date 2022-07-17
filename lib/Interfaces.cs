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

    public interface IAppOperator
    {
        Session CreateSession(AuthInfo auth);
        Session GetSession(AuthInfo auth);

        /* FORNOW
        void StoreMessage(Message msg);
        List<Message> GetMessages(string email);
        Message GetMessage(string token);
        */
    }
}
