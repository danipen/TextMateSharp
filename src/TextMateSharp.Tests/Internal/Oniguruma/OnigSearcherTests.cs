using NUnit.Framework;
using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Tests.Internal.Oniguruma
{
    class OnigSearcherTests
    {
        [Test]
        public void TestOnigSearcherNoResult()
        {
            string regexp = "whatever";
            OnigSearcher searcher = new OnigSearcher(new string[] { regexp });

            string text = "other";

            Assert.IsNull(searcher.Search(text, 0));
        }

        [Test]
        public void TestOnigSearcher()
        {
            string regexp = "[鬼]";
            OnigSearcher searcher = new OnigSearcher(new string[] { regexp });

            string text = "鬼AAA";

            // finds a double-byte match at location 0
            OnigResult result = searcher.Search(text, 0);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(0, result.LocationAt(0));
            Assert.AreEqual(1, result.LengthAt(0));

            // start searching at index 1, it should find a match
            result = searcher.Search(text, 1);

            Assert.IsNull(result);
        }
    }
}
