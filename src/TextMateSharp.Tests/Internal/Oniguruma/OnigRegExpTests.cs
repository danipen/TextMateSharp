using System;

using NUnit.Framework;

using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Tests.Internal.Oniguruma
{
    class OnigRegExpTests
    {
        [Test]
        public void TestOnigRegExp()
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
        public void TestUnicodeOnigRegExp()
        {
            using (OnigRegExp regExp = new OnigRegExp("[á]+"))
            {
                OnigString str = new OnigString("00áá00");
                OnigResult result = regExp.Search(str, 0);

                Assert.AreEqual(1, result.Count());
                Assert.AreEqual(2, result.LocationAt(0));
                Assert.AreEqual(2, result.LengthAt(0));
            }
        }

        [Test]
        public void TestUnicodeCharsWithoutBraces()
        {
            string pattern = "[\\xa0-\\xF7]";

            using (ORegex oRegex = new ORegex(pattern))
            {
                Assert.IsTrue(oRegex.Valid);
            }
        }

        [Test]
        public void ConstraintUnicodePatternLenghtTest()
        {
            string pattern = "(?i)^\\s*(interface)\\s+([a-z_\\x{7f}-\\x{7fffffff}][a-z0-9_\\x{7f}-\\x{7fffffff}]*)\\s*(extends)?\\s*";

            using (ORegex oRegex = new ORegex(pattern))
            {
                Assert.IsTrue(oRegex.Valid);
            }
        }
    }
}
