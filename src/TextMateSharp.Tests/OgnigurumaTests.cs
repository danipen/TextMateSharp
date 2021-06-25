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
    }
}