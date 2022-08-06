using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

using NUnit.Framework;

namespace msgfiles
{
    public class TestClientApp : IClientApp
    {
        public bool Cancelled { get { return false; } }
        public void Log(string message)
        {
            Console.WriteLine(message);
        }
        public void Progress(double progress) { }
    }

    public class TestServerApp : IServerApp
    {
        public IServerRequestHandler RequestHandler => new MsgRequestHandler();

        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        public async Task SendChallengeTokenAsync(string email, string display, string token)
        {
            Token = token;
            await Task.FromResult(0);
        }

        public async Task SendMessageAsync(string from, string toos, string message)
        {
            Message = message;
            await Task.FromResult(0);
        }

        public Session CreateSession(Dictionary<string, string> auth)
        {
            m_session = new Session() { token = Utils.GenToken(), email = auth["email"], display = auth["display"] };
            return m_session;
        }

        public Session? GetSession(Dictionary<string, string> auth)
        {
            return m_session;
        }

        public bool DropSession(Dictionary<string, string> auth)
        {
            if (m_session != null && auth.ContainsKey("session"))
            {
                m_session = null;
                return true;
            }
            else
                return false;
        }

        public string StoreFile(string filePath)
        {
            return m_fileStore.StoreFile(filePath);
        }

        public string Token = "";
        public string Message = "";

        private Session? m_session = null;

        private FileStore m_fileStore = 
            new FileStore(Path.Combine(Environment.CurrentDirectory, "msgfileStore"));
    }

    public class ClientServerTests
    {
        [Test]
        public void TestServer()
        {
            var app = new TestServerApp();
            using (Server server = new Server(app, 9914, "test.msgfiles.io"))
            {
                new Thread(Accepter).Start(server);
                while (!server.Ready)
                    Thread.Sleep(100);
            }
        }

        public void TestClientServerConnect()
        {
            var client_app = new TestClientApp();
            var server_app = new TestServerApp();
            using (Server server = new Server(server_app, 9914, "test.msgfiles.io"))
            {
                new Thread(Accepter).Start(server);
                while (!server.Ready)
                    Thread.Sleep(100);

                using (Client client = new Client(client_app))
                {
                    bool challenge_required = 
                        client.BeginConnect("127.0.0.1", 9914, "Michael Balloni", "contact@msgfiles.io");
                    Assert.IsTrue(challenge_required);
                    while (string.IsNullOrWhiteSpace(server_app.Token))
                        Thread.Sleep(100);
                    client.ContinueConnect(server_app.Token);
                    Assert.IsTrue(!string.IsNullOrWhiteSpace(client.SessionToken));
                    client.Disconnect();

                    challenge_required =
                        client.BeginConnect("127.0.0.1", 9914, "Michael Balloni", "contact@msgfiles.io");
                    Assert.IsTrue(!challenge_required);
                    Assert.IsTrue(!string.IsNullOrWhiteSpace(client.SessionToken));
                    client.Disconnect();

                    // FORNOW
                    // Loop over X 3...
                        // Enumerate Inbox, Deleting Each Msg
                        // Test Sending Msg X 4
                        // Enumerate Inbox, Opening Each Msg
                }
            }
        }

        private void Accepter(object? obj)
        {
            if (obj == null)
                throw new Exception("Accepter arg is null");

            Server server = (Server)obj;
            server.Accept();
        }
    }
}
