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
        }
    }
}
