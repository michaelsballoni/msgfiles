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
                            message = "blah blah blah"
                        };
                    string msg_token = db.StoreMessage(send_msg, payload_file_path, "hash");

                    string recv_payload_file_path, recv_payload_file_hash;
                    Assert.IsNull(db.GetMessage("bad@bad.com", msg_token, out recv_payload_file_path, out recv_payload_file_hash));

                    msg? recv_msg = db.GetMessage(send_msg.to, msg_token, out recv_payload_file_path, out recv_payload_file_hash);
                    if (recv_msg == null) 
                        throw new NullReferenceException("recv_msg");
                    Assert.AreEqual(msg_token, recv_msg.token);
                    Assert.AreEqual(send_msg.from, recv_msg.from);
                    Assert.AreEqual(send_msg.message, recv_msg.message);
                    Assert.AreEqual("hash", recv_payload_file_hash);
                    Assert.IsTrue((DateTimeOffset.UtcNow - recv_msg.created).TotalSeconds < 10);

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
                        message = "blah blah blah"
                    };
                string msg_token = db.StoreMessage(send_msg, payload_file_path, "hash2");
                
                Thread.Sleep(1200);
                
                Assert.AreEqual(1, db.DeleteOldMessages(1));

                string recv_payload_file_path, recv_payload_file_hash;
                Assert.IsNull(db.GetMessage(send_msg.to, "pwd2", out recv_payload_file_path, out recv_payload_file_hash));
            }
        }
    }
}
