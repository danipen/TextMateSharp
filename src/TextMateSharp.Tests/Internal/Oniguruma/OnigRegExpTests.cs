using NUnit.Framework;

using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Tests.Internal.Oniguruma
{
    class OnigRegExpTests
    {
        [Test]
        public void Basic_Regex_Should_Have_Valid_Location_And_Length()
        {
            using (OnigRegExp regExp = new OnigRegExp("[A-C]+"))
            {
                string str = "abcABC123";
                OnigResult result = regExp.Search(str, 0);

                Assert.AreEqual(1, result.Count());
                Assert.AreEqual(3, result.LocationAt(0));
                Assert.AreEqual(3, result.LengthAt(0));
            }
        }

        [Test]
        public void UTF8_Regex_Should_Have_Valid_Location_And_Length()
        {
            using (OnigRegExp regExp = new OnigRegExp("[á]+"))
            {
                string str = "00áá00";
                OnigResult result = regExp.Search(str, 0);

                Assert.AreEqual(1, result.Count());
                Assert.AreEqual(2, result.LocationAt(0));
                Assert.AreEqual(2, result.LengthAt(0));
            }
        }

        [Test]
        public void Unicode_Regex_Should_Have_Valid_Location_And_Length()
        {
            string text = "\"安\"";
            string pattern = "\\\"[^\"]*\\\"";

            using (OnigRegExp regExp = new OnigRegExp(pattern))
            {
                OnigResult result = regExp.Search(text, 0);

                Assert.AreEqual(1, result.Count());
                Assert.AreEqual(0, result.LocationAt(0));
                Assert.AreEqual(3, result.LengthAt(0));
            }
        }

        [Test]
        public void Basic_Regex_Should_Have_Valid_Location_And_Length2()
        {
            string text = "string s=\"安\"";
            string pattern = "\\\"[^\"]*\\\"";

            using (OnigRegExp regExp = new OnigRegExp(pattern))
            {
                OnigResult result = regExp.Search(text, 0);

                Assert.AreEqual(1, result.Count());
                Assert.AreEqual(9, result.LocationAt(0));
                Assert.AreEqual(3, result.LengthAt(0));
            }
        }

        [Test]
        public void Unicode_Regex_Without_Braces_Should_Be_Valid()
        {
            string pattern = "[\\xa0-\\xF7]";

            using (ORegex oRegex = new ORegex(pattern))
            {
                Assert.IsTrue(oRegex.Valid);
            }
        }

        [Test]
        public void Unicode_Regex_With_Constraint_Pattern_Should_Be_Valid()
        {
            string pattern = "(?i)^\\s*(interface)\\s+([a-z_\\x{7f}-\\x{7fffffff}][a-z0-9_\\x{7f}-\\x{7fffffff}]*)\\s*(extends)?\\s*";

            using (ORegex oRegex = new ORegex(pattern))
            {
                Assert.IsTrue(oRegex.Valid);
            }
        }

        [Test]
        public void Unicode_Regex_With_Unicode_Chars_Bigger_Than_2_Bytes_Should_Be_Valid()
        {
            string pattern = "\U0001D11E";

            using (ORegex oRegex = new ORegex(pattern))
            {
                Assert.IsTrue(oRegex.Valid);
            }
        }

        [Test]
        public void Search_Twice_In_Regex_Should_Regenerate_Location_And_Length()
        {
            using (OnigRegExp regExp = new OnigRegExp("[A-C]+"))
            {
                string str1 = "abcABC123";
                string str2 = "abc123ABC";

                OnigResult result1 = regExp.Search(str1, 0);
                OnigResult result2 = regExp.Search(str2, 0);

                Assert.AreEqual(1, result1.Count());
                Assert.AreEqual(3, result1.LocationAt(0));
                Assert.AreEqual(3, result1.LengthAt(0));

                Assert.AreEqual(1, result2.Count());
                Assert.AreEqual(6, result2.LocationAt(0));
                Assert.AreEqual(3, result2.LengthAt(0));
            }
        }
    }
}
