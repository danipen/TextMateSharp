using NUnit.Framework;

using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Tests.Internal.Oniguruma
{
    class UnicodeCharEscapeTests
    {
        [Test]
        public void Unicode_Patterns_Of_Len_2_Without_Branches_Should_Be_Escaped()
        {
            Assert.AreEqual(
                "[\\x{a0}-\\x{F7}]",
                UnicodeCharEscape.AddBracesToUnicodePatterns(
                "[\\xa0-\\xF7]"));
        }

        [Test]
        public void Unicode_Patterns_Of_Len_3_Without_Branches_Should_Be_Escaped()
        {
            Assert.AreEqual(
                "[\\x{ABC}-\\x{F77}]",
                UnicodeCharEscape.AddBracesToUnicodePatterns(
                "[\\xABC-\\xF77]"));
        }

        [Test]
        public void Unicode_Patterns_Of_Len_4_Without_Branches_Should_Be_Escaped3()
        {

            Assert.AreEqual(
                "[\\x{ABCD}-\\x{F777}]",
                UnicodeCharEscape.AddBracesToUnicodePatterns(
                "[\\xABCD-\\xF777]"));
        }

        [Test]
        public void Several_Unicode_Patterns_Without_Branches_Should_Be_Escaped()
        {

            Assert.AreEqual(
                "\\A(?:\\x{EF}\\x{BB}\\x{BF}) ? (? i : (?=\\s* @charset\\b))",
                UnicodeCharEscape.AddBracesToUnicodePatterns(
                "\\A(?:\\xEF\\xBB\\xBF) ? (? i : (?=\\s* @charset\\b))"));
        }

        [Test]
        public void Unicode_Patterns_Of_Len_7_Without_Branches_Should_Be_Escaped5()
        {

            Assert.AreEqual(
                "(?i)^\\s*(interface)\\s+([a-z_\\x{7f}-\\x{7ffffff}][a-z0-9_\\x{7f}-\\x{7ffffff}]*)\\s*(extends)?\\s*",
                UnicodeCharEscape.AddBracesToUnicodePatterns(
                "(?i)^\\s*(interface)\\s+([a-z_\\x7f-\\x7ffffff][a-z0-9_\\x7f-\\x7ffffff]*)\\s*(extends)?\\s*"));
        }

        [Test]
        public void Unicode_Patterns_Of_Len_8_Without_Branches_Should_Be_Escaped5()
        {

            Assert.AreEqual(
                "(?i)^\\s*(interface)\\s+([a-z_\\x{7f}-\\x{7fffffff}][a-z0-9_\\x{7f}-\\x{7fffffff}]*)\\s*(extends)?\\s*",
                UnicodeCharEscape.AddBracesToUnicodePatterns(
                "(?i)^\\s*(interface)\\s+([a-z_\\x7f-\\x7fffffff][a-z0-9_\\x7f-\\x7fffffff]*)\\s*(extends)?\\s*"));
        }

        [Test]
        public void Already_Escaped_Unicode_Chars_Should_Not_Be_Escaped_Again()
        {
            Assert.AreEqual(
                "(?i)^\\s*(interface)\\s+([a-z_\\x{7f}-\\x{7fffffff}][a-z0-9_\\x{7f}-\\x{7fffffff}]*)\\s*(extends)?\\s*",
                UnicodeCharEscape.AddBracesToUnicodePatterns(
                "(?i)^\\s*(interface)\\s+([a-z_\\x{7f}-\\x{7fffffff}][a-z0-9_\\x{7f}-\\x{7fffffff}]*)\\s*(extends)?\\s*"));
        }
    }
}
