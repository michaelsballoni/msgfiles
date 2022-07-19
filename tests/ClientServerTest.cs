using System;
using System.Collections.Generic;
using System.Threading;

using NUnit.Framework;

namespace msgfiles
{
    public class TestApp : IApp
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        public void SendToken(string email, string token)
        {
            Token = token;
        }

        public Session CreateSession(Dictionary<string, string> auth)
        {
            m_session = new Session() { token = Utils.GenToken(), email = auth["email"], display = auth["display"] };
            return m_session;
        }

        public Session GetSession(Dictionary<string, string> auth)
        {
            return m_session;
        }

        public string Token = "";

        private Session m_session = null;
    }

    public class ClientServerTests
    {
        [Test]
        public void TestServer()
        {
            var app = new TestApp();
            using (Server server = new Server(app, 9914, "test.msgfiles.io"))
            {
                new Thread(Accepter).Start(server);
                while (!server.Ready)
                    Thread.Sleep(100);
            }
        }

        public void TestClientServerConnect()
        {
            var app = new TestApp();
            using (Server server = new Server(app, 9914, "test.msgfiles.io"))
            {
                new Thread(Accepter).Start(server);
                while (!server.Ready)
                    Thread.Sleep(100);

                using (Client client = new Client(app))
                {
                    client.BeginConnectAsync("127.0.0.1", 9914, "Michael Balloni", "contact@msgfiles.io").Wait();
                    while (string.IsNullOrWhiteSpace(app.Token))
                        Thread.Sleep(100);

                    client.CompleteConnectAsync().Wait();
                    client.Disconnect();

                    client.BeginConnectAsync("127.0.0.1", 9914, "Michael Balloni", "contact@msgfiles.io").Wait();
                    while (string.IsNullOrWhiteSpace(app.Token))
                        Thread.Sleep(100);

                    client.CompleteConnectAsync().Wait();
                    client.Disconnect();
                }
            }
        }

        private void Accepter(object obj)
        {
            Server server = (Server)obj;
            server.Accept();
        }
    }
}
