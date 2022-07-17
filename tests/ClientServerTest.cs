using System;
using System.Collections.Generic;
using System.Threading;

using NUnit.Framework;

namespace msgfiles
{
    public class TestLogger : ILog
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }

    public class TestTokenSender : ITokenSender
    {
        public void SendToken(string token)
        {
            Token = token;
        }

        public string Token = "";
    }

    public class TestAppOperator : IAppOperator
    {
        public Session CreateSession(AuthInfo auth)
        {
            m_session = new Session() { Token = Utils.GenToken(), Email = auth.Email };
            return m_session;
        }

        public Session GetSession(AuthInfo auth)
        {
            return m_session;
        }

        private Session m_session = null;
    }

    public class ClientServerTests
    {
        [Test]
        public void TestServer()
        {
            ILog logger = new TestLogger();
            ITokenSender tokenSender = new TestTokenSender();
            IAppOperator appOperator = new TestAppOperator();
            using (Server server = new Server(logger, tokenSender, appOperator, 9914, "test.msgfiles.io"))
            {
                new Thread(Accepter).Start(server);
                while (!server.Ready)
                    Thread.Sleep(100);
            }
        }

        public void TestClientServerConnect()
        {
            var logger = new TestLogger();
            var tokenSender = new TestTokenSender();
            var appOperator = new TestAppOperator();
            using (Server server = new Server(logger, tokenSender, appOperator, 9914, "test.msgfiles.io"))
            {
                new Thread(Accepter).Start(server);
                while (!server.Ready)
                    Thread.Sleep(100);

                using (Client client = new Client(logger))
                {
                    client.BeginConnectAsync("127.0.0.1", 9914, "Michael Balloni", "contact@msgfiles.io").Wait();
                    while (string.IsNullOrWhiteSpace(tokenSender.Token))
                        Thread.Sleep(100);

                    client.CompleteConnectAsync().Wait();
                    client.DisconnectAsync().Wait();

                    client.BeginConnectAsync("127.0.0.1", 9914, "Michael Balloni", "contact@msgfiles.io").Wait();
                    while (string.IsNullOrWhiteSpace(tokenSender.Token))
                        Thread.Sleep(100);

                    client.CompleteConnectAsync().Wait();
                    client.DisconnectAsync().Wait();
                }
            }
        }

        private void Accepter(object obj)
        {
            Server server = (Server)obj;
            server.AcceptConnections();
        }
    }
}
