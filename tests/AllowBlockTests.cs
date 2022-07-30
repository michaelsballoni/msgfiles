using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace msgfiles
{
    public class AllowBlockTests
    {
        [Test]
        public void TestAllowBlock()
        {
            {
                var allow = new HashSet<string>();
                var block = new HashSet<string>();
                var ab = new AllowBlock();
                ab.SetLists(allow, block);
                try
                {
                    ab.EnsureEmailAllowed("a@b.com");
                }
                catch (InputException)
                {
                    Assert.Fail();
                }
            }

            {
                var allow = new HashSet<string>(new[] {"a@b.com"});
                var block = new HashSet<string>();
                var ab = new AllowBlock();
                ab.SetLists(allow, block);
                try
                {
                    ab.EnsureEmailAllowed("a@b.com");
                }
                catch (InputException)
                {
                    Assert.Fail();
                }
            }

            {
                var allow = new HashSet<string>(new[] { "f@b.com" });
                var block = new HashSet<string>();
                var ab = new AllowBlock();
                ab.SetLists(allow, block);
                try
                {
                    ab.EnsureEmailAllowed("a@b.com");
                    Assert.Fail();
                }
                catch (InputException)
                {
                }
            }

            {
                var allow = new HashSet<string>();
                var block = new HashSet<string>(new[] { "a@b.com" });
                var ab = new AllowBlock();
                ab.SetLists(allow, block);
                try
                {
                    ab.EnsureEmailAllowed("a@b.com");
                    Assert.Fail();
                }
                catch (InputException)
                {
                }
            }

            {
                var allow = new HashSet<string>();
                var block = new HashSet<string>(new[] { "@b.com" });
                var ab = new AllowBlock();
                ab.SetLists(allow, block);
                try
                {
                    ab.EnsureEmailAllowed("a@b.com");
                    Assert.Fail();
                }
                catch (InputException)
                {
                }
            }

            {
                var allow = new HashSet<string>(new[] { "@b.com" });
                var block = new HashSet<string>();
                var ab = new AllowBlock();
                ab.SetLists(allow, block);
                try
                {
                    ab.EnsureEmailAllowed("a@b.com");
                }
                catch (InputException)
                {
                    Assert.Fail();
                }
            }

            {
                var allow = new HashSet<string>(new[] { "a@b.com" });
                var block = new HashSet<string>(new[] { "@b.com" });
                var ab = new AllowBlock();
                ab.SetLists(allow, block);
                try
                {
                    ab.EnsureEmailAllowed("a@b.com");
                }
                catch (InputException)
                {
                    Assert.Fail();
                }
            }

            {
                var allow = new HashSet<string>(new[] { "@b.com" });
                var block = new HashSet<string>(new[] { "a@b.com" });
                var ab = new AllowBlock();
                ab.SetLists(allow, block);
                try
                {
                    ab.EnsureEmailAllowed("a@b.com");
                    Assert.Fail();
                }
                catch (InputException)
                {
                }
            }
        }
    }
}
