using NUnit.Framework;
using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Tests
{
    class OnigurumaTests
    {
        [Test]
        public void TestORegex()
        {
            using (OnigRegExp regExp = new OnigRegExp("[A-C]+"))
            {
                OnigString str = new OnigString("abcABC123"); 
                OnigResult result = regExp.Search(str, 0);

                Assert.AreEqual(1, result.Count());
                Assert.AreEqual(3, result.LocationAt(0));
                Assert.AreEqual(3, result.LengthAt(0));
            }
        }

        [Test]
        public void TestUnicodeORegex()
        {
            using (OnigRegExp regExp = new OnigRegExp("[с]+"))
            {
                OnigString str = new OnigString("00сс00");
                OnigResult result = regExp.Search(str, 0);

                Assert.AreEqual(1, result.Count());
                Assert.AreEqual(2, result.LocationAt(0));
                Assert.AreEqual(2, result.LengthAt(0));
            }
        }

        [Test]
        public void TestOnigScanner()
        {
            OnigScanner scanner = new OnigScanner(new string[] { "c", "a(b)?" });
            IOnigNextMatchResult result = scanner.FindNextMatchSync("abc", 0);

            scanner = new OnigScanner(new string[] { "a([b-d])c" });
            IOnigNextMatchResult onigResult = scanner.FindNextMatchSync("!abcdef", 0);

            var captureIndices = onigResult.GetCaptureIndices();

            Assert.AreEqual(2, captureIndices.Length);

            Assert.AreEqual(1, captureIndices[0].GetStart());
            Assert.AreEqual(3, captureIndices[0].GetLength());
            Assert.AreEqual(2, captureIndices[1].GetStart());
            Assert.AreEqual(1, captureIndices[1].GetLength());
        }
    }
}