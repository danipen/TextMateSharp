using Moq;
using NUnit.Framework;
using Onigwrap;
using System;
using TextMateSharp.Internal.Rules;

namespace TextMateSharp.Tests.Internal.Rules
{
    [TestFixture]
    public class RegExpSourceTests
    {
        [Test]
        public void ResolveBackReferences_EscapesTheCapture()
        {
            // act
            string result = Resolve(@"\1;", "a.b", CreateCapture(0, 3));

            // assert
            Assert.AreEqual(@"a\.b;", result);
        }

        [Test]
        public void ResolveBackReferences_KeepsASurrogatePair()
        {
            // act
            string result = Resolve(@"\1", "x😀", CreateCapture(1, 3));

            // assert
            Assert.AreEqual("😀", result);
        }

        [Test]
        public void ResolveBackReferences_ReplacesALoneSurrogateWithAPatternThatCompiles()
        {
            // arrange
            const string line = "\uD800 \uDE00";

            // act
            string result = Resolve(@"\1-\2", line, CreateCapture(0, 1), CreateCapture(2, 3));
            OnigScanner scanner = new OnigScanner(new[] { Resolve(@"\1", line, CreateCapture(2, 3)) });

            // assert
            Assert.AreEqual(@"(?:\p{Cs}|\x{FFFD})-(?:\p{Cs}|\x{FFFD})", result);
            Assert.AreEqual(2, scanner.FindNextMatchSync(line, 0).GetCaptureIndices()[0].Start);
            Assert.IsNotNull(scanner.FindNextMatchSync("�", 0));
        }

        private static string Resolve(string source, string line, params IOnigCaptureIndex[] captures)
        {
            IOnigCaptureIndex[] all = new IOnigCaptureIndex[captures.Length + 1];
            all[0] = CreateCapture(0, line.Length);
            captures.CopyTo(all, 1);
            return new RegExpSource(source, RuleId.Of(1)).ResolveBackReferences(line.AsMemory(), all);
        }

        private static IOnigCaptureIndex CreateCapture(int start, int end)
        {
            Mock<IOnigCaptureIndex> capture = new Mock<IOnigCaptureIndex>();
            capture.SetupGet(c => c.Start).Returns(start);
            capture.SetupGet(c => c.End).Returns(end);
            capture.SetupGet(c => c.Length).Returns(end - start);
            return capture.Object;
        }
    }
}
