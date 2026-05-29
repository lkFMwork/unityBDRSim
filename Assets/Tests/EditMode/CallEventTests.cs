using Fitzmark.BDRSim.Data;
using NUnit.Framework;

namespace Fitzmark.BDRSim.Tests
{
    public class CallEventTests
    {
        [Test]
        public void FullChanceReturnsAnEvent()
        {
            Assert.IsNotNull(CallEventLibrary.Roll(new System.Random(1), 1f));
        }

        [Test]
        public void ZeroChanceReturnsNull()
        {
            Assert.IsNull(CallEventLibrary.Roll(new System.Random(1), 0f));
        }

        [Test]
        public void LibraryHasGoodAndBadEvents()
        {
            bool hasGood = false, hasBad = false;
            foreach (var e in CallEventLibrary.All)
            {
                if (e.Good) hasGood = true; else hasBad = true;
            }
            Assert.IsTrue(hasGood);
            Assert.IsTrue(hasBad);
        }
    }
}
