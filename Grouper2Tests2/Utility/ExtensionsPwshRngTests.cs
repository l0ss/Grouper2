using NUnit.Framework;
using Grouper2.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Grouper2.Utility.Extensions.Tests
{
    [TestFixture()]
    public class ExtensionsPwshRngTests
    {
        [Test()]
        public void PwshRngTest()
        {
            var testArray = new int[]{ 0,1,2,3,4,5};

            var testarr = testArray.PwshRng(0, 2);
            Assert.AreEqual(new int[]{0,1,2}, testarr);

            testarr = testArray.PwshRng(2, 5);
            Assert.AreEqual(new int[] { 2, 3, 4,5 }, testarr);

            testarr = testArray.PwshRng(0, testArray.Length - 1);
            Assert.AreEqual(testArray, testarr);
        }
    }
}