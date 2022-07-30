using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

namespace msgfiles
{
    public class SessionDbTests
    {
        [Test]
        public void TestSessionDb()
        {
            string db_file_path = "test.db";
            if (File.Exists(db_file_path))
                File.Delete(db_file_path);

            for (int run = 1; run <= 3; ++run)
            {
                using (var db = new SessionDb(db_file_path))
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
                }
            }
        }
    }
}
