using System;
using Grouper2;
using Grouper2.Host;
using NUnit.Framework;

namespace Grouper2Tests2.Host
{
    public class SidTests
    {
        [Test]
        public void GetWellKnownSidAliasPass()
        {
            // exact match non-domain
            string testee = Sid.GetWellKnownSidAlias("S-1-16-20480");
            Assert.IsNotNull(testee);
            Assert.AreEqual("Protected Process Mandatory Level",testee);
            
            // match for a domain sid
            testee = Sid.GetWellKnownSidAlias("S-1-5-21-3623811015-3361044348-30300820-527");
            Assert.IsNotNull(testee);
            Assert.AreEqual("Enterprise Key Admins",testee);
            
            // valid sid, but just not in the list
            testee = Sid.GetWellKnownSidAlias("S-1-5-21-3623811015-3361044348-30300820");
            Assert.IsNull(testee);

        }

        [Test]
        public void GetWellKnownSidAliasMalformed()
        {
            Assert.IsNull(Sid.GetWellKnownSidAlias("this isn't a sid"));
        }

        [Test]
        public void CheckSidTestShouldPass()
        {
            // standard case that should get an exact match
            Sid testee = Sid.CheckSid("S-1-5-32-544");
            Assert.IsNotNull(testee);
            Assert.AreEqual("S-1-5-32-544", testee.Raw);
            Assert.AreEqual("", testee.Description);
            Assert.AreEqual("Administrators", testee.Name);
            Assert.AreEqual("High", testee.CanonicalPrivLevel);

            // standard case that should match based on the relativeID
            Sid testee2 = Sid.CheckSid("S-1-5-21-1234567890-500");
            Assert.IsNotNull(testee2);
            Assert.IsTrue(testee2.DomainSid);
            Assert.AreEqual("500", testee2.RelativeId);
            Assert.AreEqual("<DOMAIN>", testee2.DomainOrLocalComputerIdString);

        }

        [Test]
        public void CheckSidTestHasLetters()
        {
            // standard case that should match based on the relativeID
            Assert.IsNull(Sid.CheckSid(
                "S-1-5-21-Lol, its a string and totally not a real sid but maybe we should do something to validate it in the future-500"));
        }

        [Test]
        public void CheckSidTestDoesntMatch()
        {
            // standard case that should get no match and return a null
            Assert.IsNull(Sid.CheckSid("S-1-5-32-5434"));
        }
        [Test]
        public void CheckSidMalformedSid()
        {
            // malformed sid
            Assert.IsNull(Sid.CheckSid("123456789011121314-500"));
        }

        [Test]
        public void CheckSidValidButNotInTheList()
        {
            Assert.IsNull(Sid.CheckSid("S-1-4-200"));
        }
    }
}
