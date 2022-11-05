using NUnit.Framework;

using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Tests.Internal.Oniguruma
{
    class OnigurumaTests
    {
        [Test]
        public void Onig_Scanner_Should_Capture_Indices()
        {
            OnigScanner scanner = new OnigScanner(new[] { "c", "a(b)?" });
            IOnigNextMatchResult onigResult = scanner.FindNextMatchSync("abc", 0);

            var captureIndices = onigResult.GetCaptureIndices();

            Assert.AreEqual(2, captureIndices.Length);

            Assert.AreEqual(0, captureIndices[0].Start);
            Assert.AreEqual(2, captureIndices[0].Length);
            Assert.AreEqual(1, captureIndices[1].Start);
            Assert.AreEqual(1, captureIndices[1].Length);
        }

        [Test]
        public void Onig_Scanner_Should_Capture_Indices2()
        {
            OnigScanner scanner = new OnigScanner(new[] { "a([b-d])c" });
            IOnigNextMatchResult onigResult = scanner.FindNextMatchSync("!abcdef", 0);

            var captureIndices = onigResult.GetCaptureIndices();

            Assert.AreEqual(2, captureIndices.Length);

            Assert.AreEqual(1, captureIndices[0].Start);
            Assert.AreEqual(3, captureIndices[0].Length);
            Assert.AreEqual(2, captureIndices[1].Start);
            Assert.AreEqual(1, captureIndices[1].Length);
        }

        [Test]
        public void Onig_Scanner_Grammar_Regex_Should_Capture_Indices()
        {
            string pattern = "\\b(?:(define)|(undef))\\b\\s*\\b([_[:alpha:]][_[:alnum:]]*)\\b";
            string text = "#define VC7";

            OnigScanner scanner = new OnigScanner(new[] { pattern });
            IOnigNextMatchResult onigResult = scanner.FindNextMatchSync(text, 0);

            var captureIndices = onigResult.GetCaptureIndices();

            Assert.AreEqual(4, captureIndices.Length);

            Assert.AreEqual(
                "define VC7",
                ExtractCaptureText(text, captureIndices, 0));
            Assert.AreEqual(
                "define",
                ExtractCaptureText(text, captureIndices, 1));
            Assert.AreEqual(
                "",
                ExtractCaptureText(text, captureIndices, 2));
            Assert.AreEqual(
                "VC7",
                ExtractCaptureText(text, captureIndices, 3));
        }

        [Test]
        public void Onig_Scanner_With_Unicode_Bigger_Than_2_Bytes_Should_Capture_Indices()
        {
            string pattern = "a([b-d])\U0001D11E";
            string text = "ab\U0001D11E";

            OnigScanner scanner = new OnigScanner(new[] { pattern });
            IOnigNextMatchResult onigResult = scanner.FindNextMatchSync(text, 0);

            var captureIndices = onigResult.GetCaptureIndices();

            Assert.AreEqual(2, captureIndices.Length);

            Assert.AreEqual(
                "ab\U0001D11E",
                ExtractCaptureText(text, captureIndices, 0));

            Assert.AreEqual(
                "b",
                ExtractCaptureText(text, captureIndices, 1));
        }


        static string ExtractCaptureText(
            string text,
            IOnigCaptureIndex[] captures,
            int index)
        {
            return text.Substring(
                captures[index].Start,
                captures[index].Length);
        }
    }
}