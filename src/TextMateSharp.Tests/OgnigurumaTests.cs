using NUnit.Framework;
using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Tests
{
    public class Tests
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
    }
}