using NUnit.Framework;
using TextMateSharp.Internal.Grammars;
using TextMateSharp.Themes;

namespace TextMateSharp.Tests.Internal.Grammars
{
    [TestFixture]
    internal class EncodedTokenAttributesTests
    {
        [Test]
        public void StackElementMetadata_Test_Should_Work()
        {
            int value = EncodedTokenAttributes.Set(0, 1, StandardTokenType.RegEx, null, FontStyle.Underline | FontStyle.Bold, 101,
                    102);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.Underline | FontStyle.Bold, 101, 102);
        }

        [Test]
        public void StackElementMetadata_Should_Allow_Overwrite_Language_Id()
        {
            int value = EncodedTokenAttributes.Set(0, 1, OptionalStandardTokenType.RegEx, null, FontStyle.Underline | FontStyle.Bold, 101,
                    102);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.Underline | FontStyle.Bold, 101, 102);

            value = EncodedTokenAttributes.Set(value, 2, OptionalStandardTokenType.NotSet, null, FontStyle.NotSet, 0, 0);
            AssertMetadataHasProperties(value, 2, StandardTokenType.RegEx, false, FontStyle.Underline | FontStyle.Bold, 101, 102);
        }

        [Test]
        public void StackElementMetadata_Should_Allow_Overwrite_Token_Type()
        {
            int value = EncodedTokenAttributes.Set(0, 1, OptionalStandardTokenType.RegEx, null, FontStyle.Underline | FontStyle.Bold, 101,
                    102);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.Underline | FontStyle.Bold, 101, 102);

            value = EncodedTokenAttributes.Set(value, 0, OptionalStandardTokenType.Comment, null, FontStyle.NotSet, 0, 0);
            AssertMetadataHasProperties(value, 1, StandardTokenType.Comment, false, FontStyle.Underline | FontStyle.Bold, 101, 102);
        }

        [Test]
        public void StackElementMetadata_Should_Allow_Overwrite_Font_Style()
        {
            int value = EncodedTokenAttributes.Set(0, 1, OptionalStandardTokenType.RegEx, null, FontStyle.Underline | FontStyle.Bold, 101,
                    102);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.Underline | FontStyle.Bold, 101, 102);

            value = EncodedTokenAttributes.Set(value, 0, OptionalStandardTokenType.NotSet, null, FontStyle.None, 0, 0);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.None, 101, 102);
        }

        [Test]
        public void CanOverwriteFontStyleWithStrikethrough()
        {
            int value = EncodedTokenAttributes.Set(0, 1, OptionalStandardTokenType.RegEx, null, FontStyle.Strikethrough, 101, 102);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.Strikethrough, 101, 102);

            value = EncodedTokenAttributes.Set(value, 0, OptionalStandardTokenType.NotSet, null, FontStyle.None, 0, 0);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.None, 101, 102);
        }

        [Test]
        public void StackElementMetadata_Should_Allow_Overwrite_Foreground()
        {
            int value = EncodedTokenAttributes.Set(0, 1, OptionalStandardTokenType.RegEx, null, FontStyle.Underline | FontStyle.Bold, 101,
                    102);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.Underline | FontStyle.Bold, 101, 102);

            value = EncodedTokenAttributes.Set(value, 0, OptionalStandardTokenType.NotSet, null, FontStyle.NotSet, 5, 0);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.Underline | FontStyle.Bold, 5, 102);
        }

        [Test]
        public void StackElementMetadata_Should_Allow_Overwrite_Background()
        {
            int value = EncodedTokenAttributes.Set(0, 1, OptionalStandardTokenType.RegEx, null, FontStyle.Underline | FontStyle.Bold, 101,
                    102);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.Underline | FontStyle.Bold, 101, 102);

            value = EncodedTokenAttributes.Set(value, 0, OptionalStandardTokenType.NotSet, null, FontStyle.NotSet, 0, 7);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.Underline | FontStyle.Bold, 101, 7);
        }

        [Test]
        public void StackElementMetadata_Should_Work_At_Max_Values()
        {
            int maxLangId = 255;
            int maxTokenType = StandardTokenType.Comment | StandardTokenType.Other | StandardTokenType.RegEx
                    | StandardTokenType.String;
            FontStyle maxFontStyle = FontStyle.Bold | FontStyle.Italic | FontStyle.Underline;
            int maxForeground = 511;
            int maxBackground = 254;

            int value = EncodedTokenAttributes.Set(0, maxLangId, maxTokenType, true, maxFontStyle, maxForeground, maxBackground);
            AssertMetadataHasProperties(value, maxLangId, maxTokenType, true, maxFontStyle, maxForeground, maxBackground);
        }

        [Test]
        public void Convert_To_Binary_String_Should_Work()
        {
            string binValue1 = EncodedTokenAttributes.ToBinaryStr(EncodedTokenAttributes.Set(0, 0, 0, null, 0, 0, 511));
            Assert.AreEqual("11111111000000000000000000000000", binValue1);

            string binValue2 = EncodedTokenAttributes.ToBinaryStr(EncodedTokenAttributes.Set(0, 0, 0, null, 0, 511, 0));
            Assert.AreEqual("00000000111111111000000000000000", binValue2);
        }

        static void AssertMetadataHasProperties(
            int metadata,
            int languageId,
            /*StandardTokenType*/ int tokenType,
            bool containsBalancedBrackets,
            FontStyle fontStyle,
            int foreground,
            int background)
        {
            string actual = "{\n" +
                "languageId: " + EncodedTokenAttributes.GetLanguageId(metadata) + ",\n" +
                "tokenType: " + EncodedTokenAttributes.GetTokenType(metadata) + ",\n" +
                "containsBalancedBrackets: " + EncodedTokenAttributes.ContainsBalancedBrackets(metadata) + ",\n" +
                "fontStyle: " + EncodedTokenAttributes.GetFontStyle(metadata) + ",\n" +
                "foreground: " + EncodedTokenAttributes.GetForeground(metadata) + ",\n" +
                "background: " + EncodedTokenAttributes.GetBackground(metadata) + ",\n" +
            "}";

            string expected = "{\n" +
                    "languageId: " + languageId + ",\n" +
                    "tokenType: " + tokenType + ",\n" +
                    "containsBalancedBrackets: " + containsBalancedBrackets + ",\n" +
                    "fontStyle: " + fontStyle + ",\n" +
                    "foreground: " + foreground + ",\n" +
                    "background: " + background + ",\n" +
                "}";

            Assert.AreEqual(expected, actual, "equals for " + EncodedTokenAttributes.ToBinaryStr(metadata));
        }
    }
}
