namespace msgfiles
{
    internal class ServerApp : IServerApp
    {
        public Session CreateSession(Dictionary<string, string> auth)
        {
            string session_key = Utils.GenToken();
            var new_session =
                new Session()
                {
                    token = session_key,
                    display = auth["display"],
                    email = auth["email"]
                };
            lock (m_sessions)
                m_sessions[session_key] = new Session();
            return new_session;
        }

        public Session? GetSession(Dictionary<string, string> auth)
        {
            Session? session;
            lock (m_sessions)
            {
                if (m_sessions.TryGetValue(auth["session"], out session))
                    return session;
                else
                    return null;
            }
        }

        public void Log(string msg)
        {
            Console.WriteLine(msg);
        }

        public void SendChallengeToken(string email, string token)
        {
            // FORNOW - Just show the poor developer the token
            Console.WriteLine("Token for " + email);
            Console.WriteLine(token);
        }

        // FORNOW - Keep sessions in memory
        private Dictionary<string, Session> m_sessions = new Dictionary<string, Session>();
    }
}
