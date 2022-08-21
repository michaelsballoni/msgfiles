using System.Collections.Generic;
using System.Text;
using System.IO;

using NUnit.Framework;

namespace msgfiles
{
    public class UtilsTests
    {
        [Test]
        public void TestUtils()
        {
            string test_str = "foo foo bar blet monkey";
            byte[] test_bytes = Encoding.UTF8.GetBytes(test_str);
            string test_hex = Utils.BytesToHex(test_bytes);
            byte[] bytes_back = Utils.HexToBytes(test_hex);
            string str_back = Encoding.UTF8.GetString(bytes_back);
            Assert.AreEqual(test_str, str_back);

            string hash = Utils.HashString("foobar");
            Assert.IsTrue(!string.IsNullOrWhiteSpace(hash));

            Assert.AreEqual(6, Utils.GenChallenge().Length);
            Assert.AreEqual(48, Utils.GenToken().Length);

            var dict = new Dictionary<string, string>() { { "foo", "bar" }, { "blet", " monkey " } };
            Utils.NormalizeDict(dict, new[] { "something", "foo", "blet" });
            Assert.AreEqual("", dict["something"]);
            Assert.AreEqual("bar", dict["foo"]);
            Assert.AreEqual("monkey", dict["blet"]);

            var stream = Utils.CombineArrays(new[] { (byte)'a', (byte)'b', (byte)'c' }, new[] { (byte)'1', (byte)'2', (byte)'3', (byte)'4' });
            Assert.AreEqual("abc1234", Encoding.ASCII.GetString(stream.ToArray()));

            string enc = Utils.Encrypt("foo", "bar");
            string dec = Utils.Decrypt(enc, "bar");
            Assert.IsTrue(enc != "foo");
            Assert.IsTrue(dec != enc);
            Assert.AreEqual("foo", dec);

            Assert.AreEqual("", Utils.GetValidEmail(""));
            Assert.AreEqual("", Utils.GetValidEmail("a"));
            Assert.AreEqual("", Utils.GetValidEmail("@b"));
            Assert.AreEqual("a@b", Utils.GetValidEmail("a@b"));
            Assert.AreEqual("", Utils.GetValidEmail("a@b."));
            Assert.AreEqual("a@b.c", Utils.GetValidEmail("a@b.c"));

            {
                KeyValuePair<string, string> addr_name = new KeyValuePair<string, string>();

                try
                {
                    addr_name = Utils.ParseEmail("");
                    Assert.Fail();
                }
                catch { }

                try
                {
                    addr_name = Utils.ParseEmail(" ");
                    Assert.Fail();
                }
                catch { }

                try
                {
                    addr_name = Utils.ParseEmail("foo");
                    Assert.Fail();
                }
                catch { }

                addr_name = Utils.ParseEmail("foo@bar.com");
                Assert.AreEqual("foo@bar.com", addr_name.Key);
                Assert.AreEqual("", addr_name.Value);

                addr_name = Utils.ParseEmail("blet monkey <foo@bar.com>");
                Assert.AreEqual("foo@bar.com", addr_name.Key);
                Assert.AreEqual("blet monkey", addr_name.Value);

                try
                {
                    addr_name = Utils.ParseEmail("blet monkey <trick> <foo@bar.com>");
                    Assert.Fail();
                }
                catch (InputException) {}
                    
                Assert.AreEqual("blet@monkey.com", Utils.PrepEmailForLookup("foo bar <blet@MONKEY.com>"));

                Assert.AreEqual("foobar", Encoding.UTF32.GetString(Utils.Decompress(Utils.Compress(Encoding.UTF32.GetBytes("foobar")))));

                string sync_hash = Utils.HashStream(new MemoryStream(Encoding.UTF32.GetBytes("foobar")));
                string async_hash = Utils.HashStreamAsync(new MemoryStream(Encoding.UTF32.GetBytes("foobar"))).Result;
                Assert.AreEqual(sync_hash, async_hash);
            }
        }
    }
}
