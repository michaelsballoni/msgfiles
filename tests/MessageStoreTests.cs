using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;

using NUnit.Framework;

namespace msgfiles
{
    public class MessageStoreTests
    {
        [Test]
        public void TestMessageStore()
        {
            string db_file_path = "messages_test.db";
            if (File.Exists(db_file_path))
                File.Delete(db_file_path);

            string payload_file_path = "message_test_file_payload.txt";
            File.WriteAllText(payload_file_path, "foobar");

            for (int run = 1; run <= 3; ++run)
            {
                using (var db = new MessageStore(db_file_path))
                {
                    if (db == null)
                        throw new NullReferenceException("db");
                    msg send_msg =
                        new msg()
                        {
                            from = "foo@bar.com",
                            to = "blet@monkey.net",
                            subject = "test message",
                            body = "blah blah blah",
                            manifest = "something else"
                        };
                    string msg_token = db.StoreMessage(send_msg, payload_file_path, 1, 1.0, "");

                    string recv_payload_file_path;
                    Assert.IsNull(db.GetMessage(msg_token, "bad@bad.com", out recv_payload_file_path));

                    msg? recv_msg = db.GetMessage(msg_token, send_msg.to, out recv_payload_file_path);
                    if (recv_msg == null) 
                        throw new NullReferenceException("recv_msg");
                    Assert.AreEqual(msg_token, recv_msg.token);
                    Assert.AreEqual(send_msg.from, recv_msg.from);
                    Assert.AreEqual(send_msg.to, recv_msg.to);
                    Assert.AreEqual(send_msg.subject, recv_msg.subject);
                    Assert.AreEqual(send_msg.body, recv_msg.body);
                    Assert.AreEqual(send_msg.manifest, recv_msg.manifest);
                    Assert.IsTrue((DateTimeOffset.UtcNow - recv_msg.created).TotalSeconds < 10);

                    Assert.AreEqual(0, db.GetMessages("bad@bad.com").Count);

                    List<msg> inbox = db.GetMessages(send_msg.to);
                    Assert.AreEqual(1, inbox.Count);
                    Assert.AreEqual(msg_token, inbox[0].token);
                    Assert.AreEqual(send_msg.from, inbox[0].from);
                    Assert.AreEqual(send_msg.subject, inbox[0].subject);

                    Assert.IsFalse(db.DeleteMessage(msg_token, "bad@bad.com"));
                    Assert.IsTrue(db.DeleteMessage(msg_token, send_msg.to));
                }
            }

            using (var db = new MessageStore(db_file_path))
            {
                msg send_msg =
                    new msg()
                    {
                        from = "foo@bar.com",
                        to = "blet@monkey.net",
                        subject = "test message",
                        body = "blah blah blah",
                        manifest = "something else"
                    };
                string msg_token = db.StoreMessage(send_msg, payload_file_path, 1, 1.0, "");
                
                Thread.Sleep(2200);
                
                Assert.AreEqual(1, db.DeleteOldMessages(1));

                string recv_payload_file_path;
                Assert.IsNull(db.GetMessage(msg_token, send_msg.to, out recv_payload_file_path));
            }
        }
    }
}
