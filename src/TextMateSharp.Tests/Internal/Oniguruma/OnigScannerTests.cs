using NUnit.Framework;

using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Tests.Internal.Oniguruma
{
    class OnigurumaTests
    {
        [Test]
        public void TestOnigScanner()
        {
            OnigScanner scanner = new OnigScanner(new string[] { "c", "a(b)?" });
            IOnigNextMatchResult onigResult = scanner.FindNextMatchSync("abc", 0);

            var captureIndices = onigResult.GetCaptureIndices();

            Assert.AreEqual(2, captureIndices.Length);

            Assert.AreEqual(0, captureIndices[0].GetStart());
            Assert.AreEqual(2, captureIndices[0].GetLength());
            Assert.AreEqual(1, captureIndices[1].GetStart());
            Assert.AreEqual(1, captureIndices[1].GetLength());
        }

        [Test]
        public void TestOnigScanner2()
        {
            OnigScanner scanner = new OnigScanner(new string[] { "a([b-d])c" });
            IOnigNextMatchResult onigResult = scanner.FindNextMatchSync("!abcdef", 0);

            var captureIndices = onigResult.GetCaptureIndices();

            Assert.AreEqual(2, captureIndices.Length);

            Assert.AreEqual(1, captureIndices[0].GetStart());
            Assert.AreEqual(3, captureIndices[0].GetLength());
            Assert.AreEqual(2, captureIndices[1].GetStart());
            Assert.AreEqual(1, captureIndices[1].GetLength());
        }

        [Test]
        public void TestOnigScannerText()
        {
            string pattern = "\\b(?:(define)|(undef))\\b\\s*\\b([_[:alpha:]][_[:alnum:]]*)\\b";
            string text = "#define VC7";

            OnigScanner scanner = new OnigScanner(new string[] { pattern });
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


        static string ExtractCaptureText(
            string text,
            IOnigCaptureIndex[] captures,
            int index)
        {
            return text.Substring(
                captures[index].GetStart(),
                captures[index].GetLength());
        }
    }
}