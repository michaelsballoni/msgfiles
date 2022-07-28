using System.Collections.Generic;
using System.Text;

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

            string hash = Utils.Hash256Str("foobar");
            Assert.IsTrue(!string.IsNullOrWhiteSpace(hash));

            Assert.AreEqual(6, Utils.GenChallenge().Length);
            Assert.AreEqual(64, Utils.GenToken().Length);

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
        }
    }
}
