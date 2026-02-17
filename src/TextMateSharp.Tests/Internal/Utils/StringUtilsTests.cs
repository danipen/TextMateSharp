using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Tests.Internal.Utils
{
    [TestFixture]
    public sealed class StringUtilsTests
    {
        #region SubstringAtIndexes_String tests
        [Test]
        public void SubstringAtIndexes_String_ReturnsExpectedSubstring()
        {
            // arrange
            const string input = "abcdef";
            const int startIndex = 1;
            const int endIndex = 4;

            // act
            string result = input.SubstringAtIndexes(startIndex, endIndex);

            // assert
            Assert.AreEqual("bcd", result);
        }

        [Test]
        public void SubstringAtIndexes_String_EndEqualsStart_ReturnsEmpty()
        {
            // arrange
            const string input = "abcdef";
            const int index = 3;

            // act
            string result = input.SubstringAtIndexes(index, index);

            // assert
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void SubstringAtIndexes_String_StartGreaterThanEnd_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const string input = "abcdef";
            const int startIndex = 4;
            const int endIndex = 2;

            // act
            TestDelegate act = delegate
            {
                _ = input.SubstringAtIndexes(startIndex, endIndex);
            };

            // assert
            Assert.Throws<ArgumentOutOfRangeException>(act);
        }

        [Test]
        public void SubstringAtIndexes_String_EndBeyondLength_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const string input = "abcdef";
            const int startIndex = 2;
            const int endIndex = 10;

            // act
            TestDelegate act = delegate
            {
                _ = input.SubstringAtIndexes(startIndex, endIndex);
            };

            // assert
            Assert.Throws<ArgumentOutOfRangeException>(act);
        }
        [Test]
        public void SubstringAtIndexes_String_StartNegative_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const string input = "abcdef";
            const int startIndex = -1;
            const int endIndex = 2;

            // act
            TestDelegate act = delegate
            {
                _ = input.SubstringAtIndexes(startIndex, endIndex);
            };

            // assert
            Assert.Throws<ArgumentOutOfRangeException>(act);
        }

        [Test]
        public void SubstringAtIndexes_String_StartZero_EndLength_ReturnsFullString()
        {
            // arrange
            const string input = "abcdef";
            const int startIndex = 0;
            int endIndex = input.Length;

            // act
            string result = input.SubstringAtIndexes(startIndex, endIndex);

            // assert
            Assert.AreEqual("abcdef", result);
        }
        #endregion SubstringAtIndexes_String tests

        #region SubstringAtIndexes_ReadOnlyMemory tests
        [Test]
        public void SubstringAtIndexes_ReadOnlyMemory_StartNegative_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            ReadOnlyMemory<char> memory = "abcdef".AsMemory();
            const int startIndex = -1;
            const int endIndex = 2;

            // act
            TestDelegate act = delegate
            {
                _ = memory.SubstringAtIndexes(startIndex, endIndex);
            };

            // assert
            Assert.Throws<ArgumentOutOfRangeException>(act);
        }

        [Test]
        public void SubstringAtIndexes_ReadOnlyMemory_EndBeyondLength_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            ReadOnlyMemory<char> memory = "abcdef".AsMemory();
            const int startIndex = 1;
            const int endIndex = 10;

            // act
            TestDelegate act = delegate
            {
                _ = memory.SubstringAtIndexes(startIndex, endIndex);
            };

            // assert
            Assert.Throws<ArgumentOutOfRangeException>(act);
        }

        [Test]
        public void SubstringAtIndexes_ReadOnlyMemory_StartGreaterThanEnd_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            ReadOnlyMemory<char> memory = "abcdef".AsMemory();
            const int startIndex = 4;
            const int endIndex = 2;

            // act
            TestDelegate act = delegate
            {
                _ = memory.SubstringAtIndexes(startIndex, endIndex);
            };

            // assert
            Assert.Throws<ArgumentOutOfRangeException>(act);
        }

        [Test]
        public void SubstringAtIndexes_ReadOnlyMemory_EndEqualsStart_ReturnsEmpty()
        {
            // arrange
            ReadOnlyMemory<char> memory = "abcdef".AsMemory();
            const int index = 3;

            // act
            string result = memory.SubstringAtIndexes(index, index);

            // assert
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void SubstringAtIndexes_ReadOnlyMemory_ReturnsExpectedSubstring()
        {
            // arrange
            ReadOnlyMemory<char> memory = "abcdef".AsMemory();

            // act
            string result = memory.SubstringAtIndexes(2, 5);

            // assert
            Assert.AreEqual("cde", result);
        }
        #endregion SubstringAtIndexes_ReadOnlyMemory tests

        #region SliceAtIndexes tests
        [Test]
        public void SliceAtIndexes_ReadOnlyMemory_StartNegative_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            ReadOnlyMemory<char> memory = "abcdef".AsMemory();
            const int startIndex = -1;
            const int endIndex = 2;

            // act
            TestDelegate act = delegate
            {
                _ = memory.SliceAtIndexes(startIndex, endIndex);
            };

            // assert
            Assert.Throws<ArgumentOutOfRangeException>(act);
        }

        [Test]
        public void SliceAtIndexes_ReadOnlyMemory_StartGreaterThanEnd_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            ReadOnlyMemory<char> memory = "abcdef".AsMemory();
            const int startIndex = 4;
            const int endIndex = 2;

            // act
            TestDelegate act = delegate
            {
                _ = memory.SliceAtIndexes(startIndex, endIndex);
            };

            // assert
            Assert.Throws<ArgumentOutOfRangeException>(act);
        }

        [Test]
        public void SliceAtIndexes_ReadOnlyMemory_EndBeyondLength_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            ReadOnlyMemory<char> memory = "abcdef".AsMemory();
            const int startIndex = 1;
            const int endIndex = 10;

            // act
            TestDelegate act = delegate
            {
                _ = memory.SliceAtIndexes(startIndex, endIndex);
            };

            // assert
            Assert.Throws<ArgumentOutOfRangeException>(act);
        }

        [Test]
        public void SliceAtIndexes_ReadOnlySpan_StartNegative_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const int startIndex = -1;
            const int endIndex = 2;

            // act
            TestDelegate act = delegate
            {
                ReadOnlySpan<char> span = "abcdef".AsSpan();
                _ = span.SliceAtIndexes(startIndex, endIndex);
            };

            // assert
            Assert.Throws<ArgumentOutOfRangeException>(act);
        }

        [Test]
        public void SliceAtIndexes_ReadOnlySpan_EndBeyondLength_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const int startIndex = 1;
            const int endIndex = 10;

            // act
            TestDelegate act = delegate
            {
                ReadOnlySpan<char> span = "abcdef".AsSpan();
                _ = span.SliceAtIndexes(startIndex, endIndex);
            };

            // assert
            Assert.Throws<ArgumentOutOfRangeException>(act);
        }

        [Test]
        public void SliceAtIndexes_ReadOnlySpan_StartGreaterThanEnd_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const int startIndex = 4;
            const int endIndex = 2;

            // act
            TestDelegate act = delegate
            {
                ReadOnlySpan<char> span = "abcdef".AsSpan();
                _ = span.SliceAtIndexes(startIndex, endIndex);
            };

            // assert
            Assert.Throws<ArgumentOutOfRangeException>(act);
        }

        [Test]
        public void SliceAtIndexes_ReadOnlyMemory_ReturnsExpectedSlice()
        {
            // arrange
            ReadOnlyMemory<char> memory = "abcdef".AsMemory();
            const int startIndex = 1;
            const int endIndex = 4;

            // act
            ReadOnlyMemory<char> slice = memory.SliceAtIndexes(startIndex, endIndex);

            // assert
            Assert.AreEqual("bcd", slice.Span.ToString());
        }

        [Test]
        public void SliceAtIndexes_ReadOnlySpan_ReturnsExpectedSlice()
        {
            // arrange
            ReadOnlySpan<char> span = "abcdef".AsSpan();
            const int startIndex = 1;
            const int endIndex = 4;

            // act
            ReadOnlySpan<char> slice = span.SliceAtIndexes(startIndex, endIndex);

            // assert
            Assert.AreEqual("bcd", slice.ToString());
        }
        #endregion SliceAtIndexes tests

        #region IsValidHexColor tests
        [Test]
        public void IsValidHexColor_HashOnly_ReturnsFalse()
        {
            // arrange
            const string hex = "#";

            // act
            bool result = StringUtils.IsValidHexColor(hex);

            // assert
            Assert.False(result);
        }

        [Test]
        public void IsValidHexColor_UppercaseRgb_ReturnsTrue()
        {
            // arrange
            const string hex = "#ABC";

            // act
            bool result = StringUtils.IsValidHexColor(hex);

            // assert
            Assert.True(result);
        }

        [Test]
        public void IsValidHexColor_UppercaseRrggbb_ReturnsTrue()
        {
            // arrange
            const string hex = "#A1B2C3";

            // act
            bool result = StringUtils.IsValidHexColor(hex);

            // assert
            Assert.True(result);
        }

        [Test]
        public void IsValidHexColor_Null_ReturnsFalse()
        {
            // arrange
            const string hex = null;

            // act
            bool result = StringUtils.IsValidHexColor(hex);

            // assert
            Assert.False(result);
        }

        [Test]
        public void IsValidHexColor_Empty_ReturnsFalse()
        {
            // arrange
            const string hex = "";

            // act
            bool result = StringUtils.IsValidHexColor(hex);

            // assert
            Assert.False(result);
        }

        [Test]
        public void IsValidHexColor_ValidRgb_ReturnsTrue()
        {
            // arrange
            const string hex = "#abc";

            // act
            bool result = StringUtils.IsValidHexColor(hex);

            // assert
            Assert.True(result);
        }

        [Test]
        public void IsValidHexColor_ValidRgba_ReturnsTrue()
        {
            // arrange
            const string hex = "#abcd";

            // act
            bool result = StringUtils.IsValidHexColor(hex);

            // assert
            Assert.True(result);
        }

        [Test]
        public void IsValidHexColor_ValidRrggbb_ReturnsTrue()
        {
            // arrange
            const string hex = "#a1b2c3";

            // act
            bool result = StringUtils.IsValidHexColor(hex);

            // assert
            Assert.True(result);
        }

        [Test]
        public void IsValidHexColor_ValidRrggbbaa_ReturnsTrue()
        {
            // arrange
            const string hex = "#a1b2c3d4";

            // act
            bool result = StringUtils.IsValidHexColor(hex);

            // assert
            Assert.True(result);
        }

        [Test]
        public void IsValidHexColor_InvalidChars_ReturnsFalse()
        {
            // arrange
            const string hex = "#ggg";

            // act
            bool result = StringUtils.IsValidHexColor(hex);

            // assert
            Assert.False(result);
        }

        [Test]
        public void IsValidHexColor_MissingHash_ReturnsFalse()
        {
            // arrange
            const string hex = "abc";

            // act
            bool result = StringUtils.IsValidHexColor(hex);

            // assert
            Assert.False(result);
        }

        [Test]
        public void IsValidHexColor_TooShort_ReturnsFalse()
        {
            // arrange
            const string hex = "#12";

            // act
            bool result = StringUtils.IsValidHexColor(hex);

            // assert
            Assert.False(result);
        }

        [Test]
        public void IsValidHexColor_PrefixRgb_ReturnsTrue()
        {
            // arrange
            // regex is anchored with '^' but not '$', so a valid prefix returns true
            const string hex = "#abcTHIS_IS_NOT_HEX";

            // act
            bool result = StringUtils.IsValidHexColor(hex);

            // assert
            Assert.True(result);
        }

        [Test]
        public void IsValidHexColor_PrefixRrggbb_ReturnsTrue()
        {
            // arrange
            const string hex = "#a1b2c3ZZZ";

            // act
            bool result = StringUtils.IsValidHexColor(hex);

            // assert
            Assert.True(result);
        }

        [Test]
        public void IsValidHexColor_PrefixRrggbbaa_ReturnsTrue()
        {
            // arrange
            const string hex = "#a1b2c3d4MORE";

            // act
            bool result = StringUtils.IsValidHexColor(hex);

            // assert
            Assert.True(result);
        }

        [Test]
        public void IsValidHexColor_PrefixRgba_ReturnsTrue()
        {
            // arrange
            const string hex = "#abcdMORE";

            // act
            bool result = StringUtils.IsValidHexColor(hex);

            // assert
            Assert.True(result);
        }
        #endregion IsValidHexColor tests

        #region StrCmp tests
        [Test]
        public void StrCmp_BothNull_ReturnsZero()
        {
            // arrange
            const string a = null;
            const string b = null;

            // act
            int result = StringUtils.StrCmp(a, b);

            // assert
            Assert.AreEqual(0, result);
        }

        [Test]
        public void StrCmp_LeftNull_ReturnsMinusOne()
        {
            // arrange
            const string a = null;
            const string b = "a";

            // act
            int result = StringUtils.StrCmp(a, b);

            // assert
            Assert.AreEqual(-1, result);
        }

        [Test]
        public void StrCmp_RightNull_ReturnsOne()
        {
            // arrange
            const string a = "a";
            const string b = null;

            // act
            int result = StringUtils.StrCmp(a, b);

            // assert
            Assert.AreEqual(1, result);
        }

        [Test]
        public void StrCmp_EqualStrings_ReturnsZero()
        {
            // arrange
            const string a = "abc";
            const string b = "abc";

            // act
            int result = StringUtils.StrCmp(a, b);

            // assert
            Assert.AreEqual(0, result);
        }

        [Test]
        public void StrCmp_LessThan_ReturnsMinusOne()
        {
            // arrange
            const string a = "a";
            const string b = "b";

            // act
            int result = StringUtils.StrCmp(a, b);

            // assert
            Assert.AreEqual(-1, result);
        }

        [Test]
        public void StrCmp_GreaterThan_ReturnsOne()
        {
            // arrange
            const string a = "b";
            const string b = "a";

            // act
            int result = StringUtils.StrCmp(a, b);

            // assert
            Assert.AreEqual(1, result);
        }

        [Test]
        public void StrCmp_EqualStrings_DifferentInstances_ReturnsZero()
        {
            // arrange
            // these strings must be created at runtime to ensure they are different instances,
            // otherwise the CLR may intern them and make them reference equal
            string a = new string(['a', 'b', 'c']);
            string b = new string(['a', 'b', 'c']);

            // act
            int result = StringUtils.StrCmp(a, b);

            // assert
            Assert.AreEqual(0, result);
        }

        [Test]
        public void StrCmp_CultureEquivalentStrings_ReturnsZero()
        {
            // arrange
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
                const string a = "e\u0301"; // 'e' + combining acute
                const string b = "\u00E9";  // precomposed 'é'

                // act
                int result = StringUtils.StrCmp(a, b);

                // assert
                Assert.False(a == b);
                Assert.AreEqual(0, result);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }
        #endregion StrCmp tests

        #region StrArrCmp tests
        [Test]
        public void StrArrCmp_SameReference_ReturnsZero()
        {
            // arrange
            List<string> a = new List<string> { "a", "b" };
            List<string> b = a;

            // act
            int result = StringUtils.StrArrCmp(a, b);

            // assert
            Assert.AreEqual(0, result);
        }

        [Test]
        public void StrArrCmp_BothNull_ReturnsZero()
        {
            // arrange
            List<string> a = null;
            List<string> b = null;

            // act
            int result = StringUtils.StrArrCmp(a, b);

            // assert
            Assert.AreEqual(0, result);
        }

        [Test]
        public void StrArrCmp_LeftNull_ReturnsMinusOne()
        {
            // arrange
            List<string> a = null;
            List<string> b = new List<string> { "a" };

            // act
            int result = StringUtils.StrArrCmp(a, b);

            // assert
            Assert.AreEqual(-1, result);
        }

        [Test]
        public void StrArrCmp_RightNull_ReturnsOne()
        {
            // arrange
            List<string> a = new List<string> { "a" };
            List<string> b = null;

            // act
            int result = StringUtils.StrArrCmp(a, b);

            // assert
            Assert.AreEqual(1, result);
        }

        [Test]
        public void StrArrCmp_SameLength_AllEqual_ReturnsZero()
        {
            // arrange
            List<string> a = new List<string> { "a", "b" };
            List<string> b = new List<string> { "a", "b" };

            // act
            int result = StringUtils.StrArrCmp(a, b);

            // assert
            Assert.AreEqual(0, result);
        }

        [Test]
        public void StrArrCmp_SameLength_DiffElement_ReturnsMinusOne()
        {
            // arrange
            List<string> a = new List<string> { "a", "b" };
            List<string> b = new List<string> { "a", "c" };

            // act
            int result = StringUtils.StrArrCmp(a, b);

            // assert
            Assert.AreEqual(-1, result);
        }

        [Test]
        public void StrArrCmp_SameLength_NullElement_UsesStrCmpRules_ReturnsMinusOne()
        {
            // arrange
            List<string> a = new List<string> { "a", null };
            List<string> b = new List<string> { "a", "b" };

            // act
            int result = StringUtils.StrArrCmp(a, b);

            // assert
            Assert.AreEqual(-1, result);
        }

        [Test]
        public void StrArrCmp_DifferentLengths_ReturnsLengthDifference()
        {
            // arrange
            List<string> a = new List<string> { "a", "b", "c" };
            List<string> b = new List<string> { "a" };

            // act
            int result = StringUtils.StrArrCmp(a, b);

            // assert
            Assert.AreEqual(2, result);
        }
        #endregion StrArrCmp tests
    }
}
