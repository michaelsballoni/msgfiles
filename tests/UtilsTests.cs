using System.Collections.Generic;

using NUnit.Framework;

namespace msgfiles
{
    public class UtilsTests
    {
        [Test]
        public void TestUtils()
        {
            string hash = Utils.Hash256Str("foobar");
            Assert.IsTrue(!string.IsNullOrWhiteSpace(hash));

            var dict = new Dictionary<string, string>() { { "foo", "bar" }, { "blet", " monkey " } };
            Utils.NormalizeDict(dict, new[] { "something", "foo", "blet" });
            Assert.AreEqual("", dict["something"]);
            Assert.AreEqual("bar", dict["foo"]);
            Assert.AreEqual("monkey", dict["blet"]);
        }
    }
}
