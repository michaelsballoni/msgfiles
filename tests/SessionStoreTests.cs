﻿using System.Threading;
using System.IO;

using NUnit.Framework;

namespace msgfiles
{
    public class SessionStoreTests
    {
        [Test]
        public void TestSessionStore()
        {
            string db_file_path = "session_test.db";
            if (File.Exists(db_file_path))
                File.Delete(db_file_path);

            for (int run = 1; run <= 3; ++run)
            {
                using (var db = new SessionStore(db_file_path))
                {
                    Session? session;

                    session = db.GetSession("foobar");
                    Assert.IsNull(session);

                    var new_session = db.CreateSession("a@b.c", "blet monkey");

                    for (int get = 1; get <= 3; ++get)
                    {
                        session = db.GetSession(new_session.token);
                        Assert.IsNotNull(session);
                        if (session == null)
                            return;

                        Assert.AreEqual(new_session.token, session.token);
                        Assert.AreEqual("a@b.c", session.email);
                        Assert.AreEqual("blet monkey", session.display);
                    }

                    if (session != null)
                    {
                        Assert.IsTrue(db.DropSession(session.token));
                        session = db.GetSession(new_session.token);
                        Assert.IsNull(session);
                    }
                    else
                        Assert.Fail();

                    Assert.AreEqual(0, db.DropOldSessions(0));
                }
            }

            using (var db = new SessionStore(db_file_path))
            {
                var new_session = db.CreateSession("f@g.h", "something else");
                Thread.Sleep(1200);
                Assert.AreEqual(1, db.DropOldSessions(1));
                var session = db.GetSession(new_session.token);
                Assert.IsNull(session);
            }
        }
    }
}
