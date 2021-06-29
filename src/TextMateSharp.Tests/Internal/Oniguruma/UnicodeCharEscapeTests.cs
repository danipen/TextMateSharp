using NUnit.Framework;

using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Tests.Internal.Oniguruma
{
    class UnicodeCharEscapeTests
    {
        [Test]
        public void AddBranchesToUnicodeCharExpressionTests()
        {
            Assert.AreEqual(
                "[\\x{a0}-\\x{F7}]",
                UnicodeCharEscape.AddBracesToUnicodePatterns(
                "[\\xa0-\\xF7]"));

            Assert.AreEqual(
                "[\\x{ABC}-\\x{F77}]",
                UnicodeCharEscape.AddBracesToUnicodePatterns(
                "[\\xABC-\\xF77]"));

            Assert.AreEqual(
                "[\\x{ABCD}-\\x{F777}]",
                UnicodeCharEscape.AddBracesToUnicodePatterns(
                "[\\xABCD-\\xF777]"));

            Assert.AreEqual(
                "\\A(?:\\x{EF}\\x{BB}\\x{BF}) ? (? i : (?=\\s* @charset\\b))",
                UnicodeCharEscape.AddBracesToUnicodePatterns(
                "\\A(?:\\xEF\\xBB\\xBF) ? (? i : (?=\\s* @charset\\b))"));
        }
    }
}
