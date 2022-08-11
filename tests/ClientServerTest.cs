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
        public bool ConfirmDownload(string from, string subject, string body)
        {
            ConfirmedFrom = from;
            ConfirmedSubject = subject;
            ConfirmedBody = body;
            return true;
        }
        public bool ConfirmExtraction(string manifest, int fileCount, long totalSizeBytes, out string extractionFolder)
        {
            extractionFolder = ExtractionDirPath;
            return true;
        }

        public string ConfirmedFrom = "";
        public string ConfirmedSubject = "";
        public string ConfirmedBody = "";

        public string ExtractionDirPath = Path.Combine(Environment.CurrentDirectory, "test_extraction_dir");
    }

    public class TestServerApp : IServerApp
    {
        public IServerRequestHandler RequestHandler =>
            new MsgRequestHandler(m_allowBlock, m_fileStore, m_messageStore);

        public TestServerApp()
        {
            string msg_file_store_dir_path = Path.Combine(Environment.CurrentDirectory, "msgfileStore");
            if (Directory.Exists(msg_file_store_dir_path))
                Directory.Delete(msg_file_store_dir_path, true);
            m_fileStore = new FileStore(msg_file_store_dir_path);

            string msg_store_db_file_path = Path.Combine(Environment.CurrentDirectory, "messages.db");
            if (File.Exists(msg_store_db_file_path))
                File.Delete(msg_store_db_file_path);
            m_messageStore = new MessageStore(msg_store_db_file_path);
        }

        public void Log(string message)
        {
            Console.WriteLine(message);
        }

        public async Task SendChallengeTokenAsync(string email, string display, string token)
        {
            Token = token;
            await Task.FromResult(0);
        }

        public async Task SendMailDeliveryMessageAsync(string from, string toos, string subject, string body, string pwd)
        {
            Pwd = pwd;
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
        public string Pwd = "";

        private Session? m_session = null;

        private FileStore m_fileStore;
        private MessageStore m_messageStore;

        private AllowBlock m_allowBlock = new AllowBlock();
    }

    public class ClientServerTests
    {
        private static object sm_testLock = new object();
        [Test]
        public void TestServer()
        {
            lock (sm_testLock)
            {
                var app = new TestServerApp();
                using (Server server = new Server(app, 9914, "test.msgfiles.io"))
                {
                    new Thread(Accepter).Start(server);
                    while (!server.Ready)
                        Thread.Sleep(100);
                }
            }
        }

        [Test]
        public void TestClientServerConnect()
        {
            lock (sm_testLock)
            {
                string test_file_path = Path.Combine(Environment.CurrentDirectory, "test.txt");
                string test_file_contents = Guid.NewGuid().ToString();
                File.WriteAllText(test_file_path, test_file_contents);

                var client_app = new TestClientApp();
                var server_app = new TestServerApp();

                using (Server server = new Server(server_app, 9914, "test.msgfiles.io"))
                {
                    new Thread(Accepter).Start(server);
                    while (!server.Ready)
                        Thread.Sleep(100);

                    using (var client = new MsgClient(client_app))
                    {
                        bool challenge_required =
                            client.BeginConnect("127.0.0.1", 9914, "Contact", "contact@msgfiles.io");
                        Assert.IsTrue(challenge_required);
                        while (string.IsNullOrWhiteSpace(server_app.Token))
                            Thread.Sleep(100);
                        client.ContinueConnect(server_app.Token);
                        Assert.IsTrue(!string.IsNullOrWhiteSpace(client.SessionToken));
                        client.Disconnect();

                        challenge_required =
                            client.BeginConnect("127.0.0.1", 9914, "Contact", "contact@msgfiles.io");
                        Assert.IsTrue(!challenge_required);
                        Assert.IsTrue(!string.IsNullOrWhiteSpace(client.SessionToken));

                        Assert.IsTrue(client.SendMsg(new[] { "To <contact@msgfiles.io>" }, "test msg", "body", new[] { test_file_path }));

                        if (Directory.Exists(client_app.ExtractionDirPath))
                            Directory.Delete(client_app.ExtractionDirPath, true);
                        Directory.CreateDirectory(client_app.ExtractionDirPath);

                        Assert.IsTrue(client.GetMessage(server_app.Pwd));

                        Assert.AreEqual(client_app.ConfirmedFrom, "Contact <contact@msgfiles.io>");
                        Assert.AreEqual(client_app.ConfirmedSubject, "test msg");
                        Assert.AreEqual(client_app.ConfirmedBody, "body");

                        var test_files = Directory.GetFiles(client_app.ExtractionDirPath);
                        Assert.AreEqual(1, test_files.Length);
                        Assert.AreEqual(test_file_contents, File.ReadAllText(test_files[0]));

                        Assert.IsFalse(client.GetMessage(server_app.Pwd));

                        Assert.IsTrue(client.SendMsg(new[] { "To <contact@msgfiles.io>" }, "test msg", "body", new[] { test_file_path }));
                        Assert.IsTrue(client.DeleteMessage(server_app.Token));
                        Assert.IsFalse(client.GetMessage(server_app.Pwd));
                    }
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
