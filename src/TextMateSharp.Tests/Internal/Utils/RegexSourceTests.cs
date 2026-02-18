using Moq;
using NUnit.Framework;
using Onigwrap;
using System;
using System.Text;
using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Tests.Internal.Utils
{
    [TestFixture]
    public class RegexSourceTests
    {
        #region EscapeRegExpCharacters Tests

        [Test]
        public void EscapeRegExpCharacters_EscapesAllSpecialCharacters()
        {
            // arrange
            const string input = @"a-b\c{d}e*f+g?h|i^j$k.l,m[n]o(p)q#r";
            const string expected = @"a\-b\\c\{d\}e\*f\+g\?h\|i\^j\$k\.l\,m\[n\]o\(p\)q\#r";

            // act
            string result = RegexSource.EscapeRegExpCharacters(input);

            // assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void EscapeRegExpCharacters_DoesNotEscapeWhitespace()
        {
            // arrange
            const string input = "a b\tc\nd";
            const string expected = "a b\tc\nd";

            // act
            string result = RegexSource.EscapeRegExpCharacters(input);

            // assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void EscapeRegExpCharacters_OnlySpecialCharacters_EscapesAll()
        {
            // arrange
            const string input = @"-\{}*+?|^$.,[]()#";
            const string expected = @"\-\\\{\}\*\+\?\|\^\$\.\,\[\]\(\)\#";

            // act
            string result = RegexSource.EscapeRegExpCharacters(input);

            // assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void EscapeRegExpCharacters_ConsecutiveSpecialCharacters_EscapesEach()
        {
            // arrange
            const string input = "++--";
            const string expected = @"\+\+\-\-";

            // act
            string result = RegexSource.EscapeRegExpCharacters(input);

            // assert
            Assert.AreEqual(expected, result);
        }

        [TestCase("-", "\\-")]
        [TestCase("\\", "\\\\")]
        [TestCase("{", "\\{")]
        [TestCase("}", "\\}")]
        [TestCase("*", "\\*")]
        [TestCase("+", "\\+")]
        [TestCase("?", "\\?")]
        [TestCase("|", "\\|")]
        [TestCase("^", "\\^")]
        [TestCase("$", "\\$")]
        [TestCase(".", "\\.")]
        [TestCase(",", "\\,")]
        [TestCase("[", "\\[")]
        [TestCase("]", "\\]")]
        [TestCase("(", "\\(")]
        [TestCase(")", "\\)")]
        [TestCase("#", "\\#")]
        public void EscapeRegExpCharacters_SingleSpecialCharacter_Escapes(string input, string expected)
        {
            // act
            string result = RegexSource.EscapeRegExpCharacters(input);

            // assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void EscapeRegExpCharacters_TwoSpecialCharacters_EscapesBoth()
        {
            // arrange
            const string input = "[]";
            const string expected = @"\[\]";

            // act
            string result = RegexSource.EscapeRegExpCharacters(input);

            // assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void EscapeRegExpCharacters_ExtremelyLongString_EscapesAll()
        {
            // arrange
            // Build the expected result using a simple StringBuilder-based approach without substring allocations.
            // this is intentionally straightforward test code rather than a fully span-optimized implementation
            const int length = 10_000;
            string input = new string('-', length);
            StringBuilder expectedBuilder = new StringBuilder(length * 2);
            for (int i = 0; i < length; i++)
            {
                expectedBuilder.Append('\\');
                expectedBuilder.Append('-');
            }

            // act
            string result = RegexSource.EscapeRegExpCharacters(input);

            // assert
            Assert.AreEqual(expectedBuilder.ToString(), result);
        }

        [Test]
        public void EscapeRegExpCharacters_Empty_ReturnsEmpty()
        {
            // arrange
            const string input = "";

            // act
            string result = RegexSource.EscapeRegExpCharacters(input);

            // assert
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void EscapeRegExpCharacters_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => RegexSource.EscapeRegExpCharacters(null));
        }

        [Test]
        public void EscapeRegExpCharacters_SingleNonSpecialCharacter_ReturnsUnchanged()
        {
            // arrange
            const string input = "a";

            // act
            string result = RegexSource.EscapeRegExpCharacters(input);

            // assert
            Assert.AreEqual("a", result);
        }

        #endregion EscapeRegExpCharacters Tests

        #region HasCaptures Tests

        [Test]
        public void HasCaptures_NullSource_ReturnsFalse()
        {
            // arrange
            const string input = null;

            // act
            bool result = RegexSource.HasCaptures(input);

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void HasCaptures_Empty_ReturnsFalse()
        {
            // arrange
            const string input = "";

            // act
            bool result = RegexSource.HasCaptures(input);

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void HasCaptures_NoCaptures_ReturnsFalse()
        {
            // arrange
            const string input = "abc$def";

            // act
            bool result = RegexSource.HasCaptures(input);

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void HasCaptures_NumericCapture_ReturnsTrue()
        {
            // arrange
            const string input = "value $1 end";

            // act
            bool result = RegexSource.HasCaptures(input);

            // assert
            Assert.IsTrue(result);
        }

        [Test]
        public void HasCaptures_CommandCapture_ReturnsTrue()
        {
            // arrange
            const string input = "value ${2:/downcase} end";

            // act
            bool result = RegexSource.HasCaptures(input);

            // assert
            Assert.IsTrue(result);
        }

        [TestCase("value $a end")]
        [TestCase("value $-1 end")]
        [TestCase("value $ end")]
        public void HasCaptures_MalformedNumeric_ReturnsFalse(string input)
        {
            // act
            bool result = RegexSource.HasCaptures(input);

            // assert
            Assert.IsFalse(result);
        }

        [TestCase("value ${2:/invalid} end")]
        [TestCase("value ${2:/} end")]
        [TestCase("value ${2:/upcase end")]
        [TestCase("value ${2:/{downcase}} end")]
        public void HasCaptures_MalformedCommand_ReturnsFalse(string input)
        {
            // act
            bool result = RegexSource.HasCaptures(input);

            // assert
            Assert.IsFalse(result);
        }

        [Test]
        public void HasCaptures_NumericCaptureInMalformedCommand_ReturnsTrue()
        {
            // arrange
            const string input = "value $2:/upcase} end";

            // act
            bool result = RegexSource.HasCaptures(input);

            // assert
            Assert.IsTrue(result);
        }

        #endregion HasCaptures Tests

        #region ReplaceCaptures Tests

        [Test]
        public void ReplaceCaptures_NoMatches_ReturnsOriginalString()
        {
            // arrange
            const string regexSource = "plain text";
            ReadOnlyMemory<char> captureSource = "value".AsMemory();

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, []);

            // assert
            Assert.AreEqual(regexSource, result);
        }

        [Test]
        public void ReplaceCaptures_ReplacesNumericCaptures()
        {
            // arrange
            const string regexSource = "Hello $1 $2";
            ReadOnlyMemory<char> captureSource = "alpha beta".AsMemory();
            IOnigCaptureIndex[] captureIndices =
            [
                CreateCapture(0, 10),
                CreateCapture(0, 5),
                CreateCapture(6, 10)
            ];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("Hello alpha beta", result);
        }

        [Test]
        public void ReplaceCaptures_ReplacesCommandCapturesWithCaseTransformations()
        {
            // arrange
            const string regexSource = "Value ${1:/upcase} ${2:/downcase}";
            ReadOnlyMemory<char> captureSource = "MiXeD CaSe".AsMemory();
            IOnigCaptureIndex[] captureIndices =
            [
                CreateCapture(0, 10),
                CreateCapture(0, 5),
                CreateCapture(6, 10)
            ];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("Value MIXED case", result);
        }

        [Test]
        public void ReplaceCaptures_AllowsCaptureZero()
        {
            // arrange
            const string regexSource = "start $0 end";
            ReadOnlyMemory<char> captureSource = "alpha".AsMemory();
            IOnigCaptureIndex[] captureIndices =
            [
                CreateCapture(0, 5)
            ];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("start alpha end", result);
        }

        [Test]
        public void ReplaceCaptures_ReplacesMaximumCaptureIndex()
        {
            // arrange
            const string regexSource = "value $0 $99";
            ReadOnlyMemory<char> captureSource = "abc".AsMemory();
            IOnigCaptureIndex[] captureIndices = new IOnigCaptureIndex[100];
            captureIndices[0] = CreateCapture(0, 1);
            captureIndices[99] = CreateCapture(0, 3);

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("value a abc", result);
        }

        [Test]
        public void ReplaceCaptures_HighNumberedCaptureNullEntry_ReturnsOriginalMatch()
        {
            // arrange
            const string regexSource = "value $98";
            ReadOnlyMemory<char> captureSource = "abc".AsMemory();
            IOnigCaptureIndex[] captureIndices = new IOnigCaptureIndex[99];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("value $98", result);
        }

        [Test]
        public void ReplaceCaptures_CaptureIndexEqualToArrayLength_ReturnsOriginalMatch()
        {
            // arrange
            const string regexSource = "value $98";
            ReadOnlyMemory<char> captureSource = "abc".AsMemory();
            IOnigCaptureIndex[] captureIndices = new IOnigCaptureIndex[98];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("value $98", result);
        }

        [Test]
        public void ReplaceCaptures_OutOfBoundsCaptureIndex_ReturnsOriginalMatch()
        {
            // arrange
            const string regexSource = "value $100";
            ReadOnlyMemory<char> captureSource = "abc".AsMemory();
            IOnigCaptureIndex[] captureIndices = new IOnigCaptureIndex[100];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("value $100", result);
        }

        [Test]
        public void ReplaceCaptures_MinimumLengthArrayWithNullEntry_ReturnsOriginalMatch()
        {
            // arrange
            const string regexSource = "value $1";
            ReadOnlyMemory<char> captureSource = "abc".AsMemory();
            IOnigCaptureIndex[] captureIndices = new IOnigCaptureIndex[2];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("value $1", result);
        }

        [Test]
        public void ReplaceCaptures_CommandCaptureZero_UsesTransform()
        {
            // arrange
            const string regexSource = "value ${0:/upcase}";
            ReadOnlyMemory<char> captureSource = "abc".AsMemory();
            IOnigCaptureIndex[] captureIndices =
            [
                CreateCapture(0, 3)
            ];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("value ABC", result);
        }

        [Test]
        public void ReplaceCaptures_RemovesLeadingDotsBeforeReturning()
        {
            // arrange
            const string regexSource = "prefix $1";
            ReadOnlyMemory<char> captureSource = ".Foo".AsMemory();
            IOnigCaptureIndex[] captureIndices =
            [
                CreateCapture(0, 4),
                CreateCapture(0, 4)
            ];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("prefix Foo", result);
        }

        [Test]
        public void ReplaceCaptures_RemovesMultipleLeadingDots()
        {
            // arrange
            const string regexSource = "prefix $1";
            ReadOnlyMemory<char> captureSource = "...Foo".AsMemory();
            IOnigCaptureIndex[] captureIndices =
            [
                CreateCapture(0, 6),
                CreateCapture(0, 6)
            ];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("prefix Foo", result);
        }

        [Test]
        public void ReplaceCaptures_CaptureWithOnlyLeadingDots_ReturnsEmptyCapture()
        {
            // arrange
            const string regexSource = "prefix $1";
            ReadOnlyMemory<char> captureSource = "...".AsMemory();
            IOnigCaptureIndex[] captureIndices =
            [
                CreateCapture(0, 3),
                CreateCapture(0, 3)
            ];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("prefix ", result);
        }

        [Test]
        public void ReplaceCaptures_ZeroLengthCapture_ReplacesWithEmpty()
        {
            // arrange
            const string regexSource = "x$1y";
            ReadOnlyMemory<char> captureSource = "abc".AsMemory();
            IOnigCaptureIndex[] captureIndices =
            [
                CreateCapture(0, 3),
                CreateCapture(1, 1)
            ];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("xy", result);
        }

        [Test]
        public void ReplaceCaptures_CaptureReferencesWithEmptyIndices_ReturnsOriginalMatch()
        {
            // arrange
            const string regexSource = "value $1";
            ReadOnlyMemory<char> captureSource = "abc".AsMemory();

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, []);

            // assert
            Assert.AreEqual("value $1", result);
        }

        [Test]
        public void ReplaceCaptures_ZeroLengthCaptureAtStart_ReplacesWithEmpty()
        {
            // arrange
            const string regexSource = "value $1";
            ReadOnlyMemory<char> captureSource = "abc".AsMemory();
            IOnigCaptureIndex[] captureIndices =
            [
                null,
                CreateCapture(0, 0)
            ];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("value ", result);
        }

        [Test]
        public void ReplaceCaptures_ZeroLengthCaptureAtEnd_ReplacesWithEmpty()
        {
            // arrange
            const string regexSource = "value $1";
            ReadOnlyMemory<char> captureSource = "abc".AsMemory();
            int end = captureSource.Length;
            IOnigCaptureIndex[] captureIndices =
            [
                null,
                CreateCapture(end, end)
            ];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("value ", result);
        }

        [Test]
        public void ReplaceCaptures_MissingCaptureIndex_ReturnsOriginalMatch()
        {
            // arrange
            const string regexSource = "start $2 end";
            ReadOnlyMemory<char> captureSource = "alpha".AsMemory();
            IOnigCaptureIndex[] captureIndices =
            [
                CreateCapture(0, 5),
                CreateCapture(0, 5)
            ];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("start $2 end", result);
        }

        [TestCase("value $a end")]
        [TestCase("value $-1 end")]
        [TestCase("value $ end")]
        public void ReplaceCaptures_MalformedNumeric_ReturnsOriginal(string regexSource)
        {
            // arrange
            ReadOnlyMemory<char> captureSource = "alpha".AsMemory();

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, []);

            // assert
            Assert.AreEqual(regexSource, result);
        }

        [TestCase("value ${2:/invalid} end")]
        [TestCase("value ${2:/} end")]
        [TestCase("value ${2:/upcase end")]
        [TestCase("value $2:/upcase} end")]
        [TestCase("value ${2:/{downcase}} end")]
        public void ReplaceCaptures_MalformedCommand_ReturnsOriginal(string regexSource)
        {
            // arrange
            ReadOnlyMemory<char> captureSource = "alpha".AsMemory();

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, []);

            // assert
            Assert.AreEqual(regexSource, result);
        }

        [Test]
        public void ReplaceCaptures_NullCaptureEntry_ReturnsOriginalMatch()
        {
            // arrange
            const string regexSource = "value $1";
            ReadOnlyMemory<char> captureSource = "alpha".AsMemory();
            IOnigCaptureIndex[] captureIndices =
            [
                CreateCapture(0, 5),
                null
            ];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("value $1", result);
        }

        [Test]
        public void ReplaceCaptures_NullCaptureIndices_ReturnsOriginalMatch()
        {
            // arrange
            const string regexSource = "value $1";
            ReadOnlyMemory<char> captureSource = "alpha".AsMemory();

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, null);

            // assert
            Assert.AreEqual("value $1", result);
        }

        [Test]
        public void ReplaceCaptures_InvalidCaptureStart_Throws()
        {
            // arrange
            const string regexSource = "value $1";
            ReadOnlyMemory<char> captureSource = "alpha".AsMemory();
            IOnigCaptureIndex[] captureIndices =
            [
                CreateCapture(0, 5),
                CreateCapture(-1, 2)
            ];

            // act + assert
            Assert.Throws<ArgumentOutOfRangeException>(
                () => RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices));
        }

        [Test]
        public void ReplaceCaptures_CaptureEndBeyondLength_Throws()
        {
            // arrange
            const string regexSource = "value $1";
            ReadOnlyMemory<char> captureSource = "alpha".AsMemory();
            IOnigCaptureIndex[] captureIndices =
            [
                CreateCapture(0, 5),
                CreateCapture(0, 6)
            ];

            // act + assert
            Assert.Throws<ArgumentOutOfRangeException>(
                () => RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices));
        }

        [Test]
        public void ReplaceCaptures_CaptureStartGreaterThanEnd_Throws()
        {
            // arrange
            const string regexSource = "value $1";
            ReadOnlyMemory<char> captureSource = "alpha".AsMemory();
            IOnigCaptureIndex[] captureIndices =
            [
                CreateCapture(0, 5),
                CreateCapture(4, 2)
            ];

            // act + assert
            Assert.Throws<ArgumentOutOfRangeException>(
                () => RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices));
        }

        [Test]
        public void ReplaceCaptures_NullRegexSource_Throws()
        {
            // arrange
            ReadOnlyMemory<char> captureSource = "alpha".AsMemory();

            // act + assert
            Assert.Throws<ArgumentNullException>(
                () => RegexSource.ReplaceCaptures(null, captureSource, []));
        }

        [Test]
        public void ReplaceCaptures_NullCaptureEntryAtIndexZero_ReturnsOriginalMatch()
        {
            // arrange
            const string regexSource = "value $0";
            ReadOnlyMemory<char> captureSource = "abc".AsMemory();
            IOnigCaptureIndex[] captureIndices = new IOnigCaptureIndex[1];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("value $0", result);
        }

        [Test]
        public void ReplaceCaptures_EmptyRegexSource_ReturnsEmpty()
        {
            // arrange
            const string regexSource = "";
            ReadOnlyMemory<char> captureSource = "alpha".AsMemory();

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, []);

            // assert
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void ReplaceCaptures_EmptyCaptureSource_ReplacesWithEmpty()
        {
            // arrange
            const string regexSource = "value $1";
            ReadOnlyMemory<char> captureSource = ReadOnlyMemory<char>.Empty;
            IOnigCaptureIndex[] captureIndices =
            [
                null,
                CreateCapture(0, 0)
            ];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("value ", result);
        }

        [Test]
        public void ReplaceCaptures_CaptureAtEndOfSource_Replaces()
        {
            // arrange
            const string regexSource = "value $1";
            ReadOnlyMemory<char> captureSource = "abc".AsMemory();
            int captureEndIndex = captureSource.Length;
            IOnigCaptureIndex[] captureIndices =
            [
                null,
                CreateCapture(0, captureEndIndex)
            ];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("value abc", result);
        }

        [Test]
        public void ReplaceCaptures_CaptureZeroWithSingleElementArray_Replaces()
        {
            // arrange
            const string regexSource = "value $0";
            ReadOnlyMemory<char> captureSource = "abc".AsMemory();
            IOnigCaptureIndex[] captureIndices =
            [
                CreateCapture(0, 3)
            ];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("value abc", result);
        }

        [Test]
        public void ReplaceCaptures_CaptureZeroWithEmptyArray_ReturnsOriginalMatch()
        {
            // arrange
            const string regexSource = "value $0";
            ReadOnlyMemory<char> captureSource = "abc".AsMemory();

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, []);

            // assert
            Assert.AreEqual("value $0", result);
        }

        [Test]
        public void ReplaceCaptures_NullEntryAtLastIndex_ReturnsOriginalMatch()
        {
            // arrange
            const string regexSource = "value $2";
            ReadOnlyMemory<char> captureSource = "abc".AsMemory();
            IOnigCaptureIndex[] captureIndices = new IOnigCaptureIndex[3];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual("value $2", result);
        }

        [Test]
        public void ReplaceCaptures_MaxIntCaptureIndex_ReturnsOriginalMatch()
        {
            // arrange
            string index = int.MaxValue.ToString();
            string regexSource = $"value ${index}";
            ReadOnlyMemory<char> captureSource = "abc".AsMemory();
            IOnigCaptureIndex[] captureIndices =
            [
                CreateCapture(0, 3)
            ];

            // act
            string result = RegexSource.ReplaceCaptures(regexSource, captureSource, captureIndices);

            // assert
            Assert.AreEqual(regexSource, result);
        }

        #endregion ReplaceCaptures Tests

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