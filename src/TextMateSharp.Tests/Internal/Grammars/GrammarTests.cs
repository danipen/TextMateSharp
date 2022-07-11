using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars;
using TextMateSharp.Internal.Grammars.Reader;
using TextMateSharp.Internal.Themes.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Tests.Resources;
using TextMateSharp.Themes;

namespace TextMateSharp.Tests.Internal.Grammars
{
    class GrammarTests
    {
        [Test]
        public void StackElementMetadata_Test_Should_Work()
        {
            int value = StackElementMetadata.Set(0, 1, StandardTokenType.RegEx, null, FontStyle.Underline | FontStyle.Bold, 101,
                    102);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.Underline | FontStyle.Bold, 101, 102);
        }

        [Test]
        public void StackElementMetadata_Should_Allow_Overwrite_Language_Id()
        {
            int value = StackElementMetadata.Set(0, 1, OptionalStandardTokenType.RegEx, null, FontStyle.Underline | FontStyle.Bold, 101,
                    102);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.Underline | FontStyle.Bold, 101, 102);

            value = StackElementMetadata.Set(value, 2, OptionalStandardTokenType.NotSet, null, FontStyle.NotSet, 0, 0);
            AssertMetadataHasProperties(value, 2, StandardTokenType.RegEx, false, FontStyle.Underline | FontStyle.Bold, 101, 102);
        }

        [Test]
        public void StackElementMetadata_Should_Allow_Overwrite_Token_Type()
        {
            int value = StackElementMetadata.Set(0, 1, OptionalStandardTokenType.RegEx, null, FontStyle.Underline | FontStyle.Bold, 101,
                    102);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.Underline | FontStyle.Bold, 101, 102);

            value = StackElementMetadata.Set(value, 0, OptionalStandardTokenType.Comment, null, FontStyle.NotSet, 0, 0);
            AssertMetadataHasProperties(value, 1, StandardTokenType.Comment, false, FontStyle.Underline | FontStyle.Bold, 101, 102);
        }

        [Test]
        public void StackElementMetadata_Should_Allow_Overwrite_Font_Style()
        {
            int value = StackElementMetadata.Set(0, 1, OptionalStandardTokenType.RegEx, null, FontStyle.Underline | FontStyle.Bold, 101,
                    102);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.Underline | FontStyle.Bold, 101, 102);

            value = StackElementMetadata.Set(value, 0, OptionalStandardTokenType.NotSet, null, FontStyle.None, 0, 0);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.None, 101, 102);
        }

        [Test]
        public void CanOverwriteFontStyleWithStrikethrough()
        {
            int value = StackElementMetadata.Set(0, 1, OptionalStandardTokenType.RegEx, null, FontStyle.Strikethrough, 101, 102);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.Strikethrough, 101, 102);

            value = StackElementMetadata.Set(value, 0, OptionalStandardTokenType.NotSet, null, FontStyle.None, 0, 0);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.None, 101, 102);
        }

        [Test]
        public void StackElementMetadata_Should_Allow_Overwrite_Foreground()
        {
            int value = StackElementMetadata.Set(0, 1, OptionalStandardTokenType.RegEx, null, FontStyle.Underline | FontStyle.Bold, 101,
                    102);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.Underline | FontStyle.Bold, 101, 102);

            value = StackElementMetadata.Set(value, 0, OptionalStandardTokenType.NotSet, null, FontStyle.NotSet, 5, 0);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.Underline | FontStyle.Bold, 5, 102);
        }

        [Test]
        public void StackElementMetadata_Should_Allow_Overwrite_Background()
        {
            int value = StackElementMetadata.Set(0, 1, OptionalStandardTokenType.RegEx, null, FontStyle.Underline | FontStyle.Bold, 101,
                    102);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.Underline | FontStyle.Bold, 101, 102);

            value = StackElementMetadata.Set(value, 0, OptionalStandardTokenType.NotSet, null, FontStyle.NotSet, 0, 7);
            AssertMetadataHasProperties(value, 1, StandardTokenType.RegEx, false, FontStyle.Underline | FontStyle.Bold, 101, 7);
        }

        [Test]
        public void StackElementMetadata_Should_Work_At_Max_Values()
        {
            int maxLangId = 255;
            int maxTokenType = StandardTokenType.Comment | StandardTokenType.Other | StandardTokenType.RegEx
                    | StandardTokenType.String;
            int maxFontStyle = FontStyle.Bold | FontStyle.Italic | FontStyle.Underline;
            int maxForeground = 511;
            int maxBackground = 254;

            int value = StackElementMetadata.Set(0, maxLangId, maxTokenType, true, maxFontStyle, maxForeground, maxBackground);
            AssertMetadataHasProperties(value, maxLangId, maxTokenType, true, maxFontStyle, maxForeground, maxBackground);
        }

        [Test]
        public void Convert_To_Binary_String_Should_Work()
        {
            string binValue1 = StackElementMetadata.ToBinaryStr(StackElementMetadata.Set(0, 0, 0, null, 0, 0, 511));
            Assert.AreEqual("11111111000000000000000000000000", binValue1);

            string binValue2 = StackElementMetadata.ToBinaryStr(StackElementMetadata.Set(0, 0, 0, null, 0, 511, 0));
            Assert.AreEqual("00000000111111111000000000000000", binValue2);
        }

        [Test]
        public void Parse_Simple_Tokens_Should_Work()
        {
            string line = "using System;";

            Registry.Registry registry = new Registry.Registry(
                new TestRegistry());

            IGrammar grammar = registry.LoadGrammar("source.cs");

            ITokenizeLineResult lineTokens = grammar.TokenizeLine(line);

            IToken[] tokens = lineTokens.Tokens;

            Assert.AreEqual(4, tokens.Length);

            AssertTokenValuesAreEqual(tokens[0],
                0, 5, "source.cs", "keyword.other.using.cs");
            AssertTokenValuesAreEqual(tokens[1],
                5, 6, "source.cs");
            AssertTokenValuesAreEqual(tokens[2],
                6, 12, "source.cs", "entity.name.type.namespace.cs");
            AssertTokenValuesAreEqual(tokens[3],
                12, 13, "source.cs", "punctuation.terminator.statement.cs");
        }

        [Test]
        public void Parse_Css_Tokens_Should_Work()
        {
            string line =
                "body { margin: 25px; }";

            Registry.Registry registry = new Registry.Registry(
                new TestRegistry());

            IGrammar grammar = registry.LoadGrammar("source.css");

            ITokenizeLineResult lineTokens = grammar.TokenizeLine(line);

            IToken[] tokens = lineTokens.Tokens;

            Assert.AreEqual(12, tokens.Length);
        }

        [Test]
        public void Match_Scope_Name_Should_Work()
        {
            Registry.Registry registry = new Registry.Registry(
                new TestRegistry());

            IGrammar grammar = registry.LoadGrammar("source.css");

            Theme theme = registry.GetTheme();
            List<ThemeTrieElementRule> themeRules =
                theme.Match(new string[] { "support.type.property-name.css" });

            string color = theme.GetColor(themeRules[0].foreground);

            Assert.IsFalse(string.IsNullOrEmpty(color));
        }

        [Test]
        public void Batch_Grammar_Should_Work()
        {
            string line =
                "REM echo off";

            Registry.Registry registry = new Registry.Registry(
                new TestRegistry());

            IGrammar grammar = registry.LoadGrammar("source.batchfile");

            ITokenizeLineResult lineTokens = grammar.TokenizeLine(line);

            Assert.AreEqual(2, lineTokens.Tokens.Length);
        }

        [Test]
        public void Multiline_Tokens_Should_Work()
        {
            string[] lines = new string[]
            {
                "public int Compute()",
                "{",
                "    return 5 + 8;",
                "}",
            };

            Registry.Registry registry = new Registry.Registry(
                new TestRegistry());

            IGrammar grammar = registry.LoadGrammar("source.cs");

            List<IToken> tokens = new List<IToken>();

            StackElement ruleStack = null;

            foreach (string line in lines)
            {
                ITokenizeLineResult lineTokens = grammar.TokenizeLine(line, ruleStack);

                ruleStack = lineTokens.RuleStack;

                tokens.AddRange(lineTokens.Tokens);
            }

            Assert.AreEqual(18, tokens.Count);

            AssertTokenValuesAreEqual(tokens[0], 0, 6, "source.cs", "storage.modifier.cs");
            AssertTokenValuesAreEqual(tokens[1], 6, 7, "source.cs");
            AssertTokenValuesAreEqual(tokens[2], 7, 10, "source.cs", "keyword.type.cs");
            AssertTokenValuesAreEqual(tokens[3], 10, 11, "source.cs");
            AssertTokenValuesAreEqual(tokens[4], 11, 18, "source.cs", "entity.name.function.cs");
            AssertTokenValuesAreEqual(tokens[5], 18, 19, "source.cs", "punctuation.parenthesis.open.cs");
            AssertTokenValuesAreEqual(tokens[6], 19, 20, "source.cs", "punctuation.parenthesis.close.cs");
            AssertTokenValuesAreEqual(tokens[7], 0, 1, "source.cs", "punctuation.curlybrace.open.cs");
            AssertTokenValuesAreEqual(tokens[8], 0, 4, "source.cs");
            AssertTokenValuesAreEqual(tokens[9], 4, 10, "source.cs", "keyword.control.flow.return.cs");
            AssertTokenValuesAreEqual(tokens[10], 10, 11, "source.cs");
            AssertTokenValuesAreEqual(tokens[11], 11, 12, "source.cs", "constant.numeric.decimal.cs");
            AssertTokenValuesAreEqual(tokens[12], 12, 13, "source.cs");
            AssertTokenValuesAreEqual(tokens[13], 13, 14, "source.cs", "keyword.operator.arithmetic.cs");
            AssertTokenValuesAreEqual(tokens[14], 14, 15, "source.cs");
            AssertTokenValuesAreEqual(tokens[15], 15, 16, "source.cs", "constant.numeric.decimal.cs");
            AssertTokenValuesAreEqual(tokens[16], 16, 17, "source.cs", "punctuation.terminator.statement.cs");
            AssertTokenValuesAreEqual(tokens[17], 0, 1, "source.cs", "punctuation.curlybrace.close.cs");
        }

        [Test]
        public void Tokenize_Unicode_Comments_Should_Work()
        {
            string text = "string s = \"chars: 安定させる\";";

            Registry.Registry registry = new Registry.Registry(
                new TestRegistry());

            IGrammar grammar = registry.LoadGrammar("source.cs");

            ITokenizeLineResult lineTokens = grammar.TokenizeLine(text);

            Assert.IsTrue(lineTokens.Tokens[7].Scopes.Contains("string.quoted.double.cs"));
            Assert.AreEqual(12, lineTokens.Tokens[7].StartIndex);
            Assert.AreEqual(24, lineTokens.Tokens[7].EndIndex);

            Assert.IsTrue(lineTokens.Tokens[8].Scopes.Contains("punctuation.definition.string.end.cs"));
            Assert.AreEqual(24, lineTokens.Tokens[8].StartIndex);
            Assert.AreEqual(25, lineTokens.Tokens[8].EndIndex);
        }

        [Test]
        public void Tokenize_Unicode_Comments_Should_Work2()
        {
            // TODO: fix parsing unicode characters
            string text = "\"安安安\"";
            //string text = "string s = \"chars: 12345\";";

            Registry.Registry registry = new Registry.Registry(
                            new TestRegistry());

            IGrammar grammar = registry.LoadGrammar("source.cs");

            ITokenizeLineResult lineTokens = grammar.TokenizeLine(text);
        }

        [Test]
        public void Grammar_Should_Inject_Other_Grammars()
        {
            Registry.Registry registry = new Registry.Registry(
                new TestRegistry());

            string line = "@Component({template:`<a href='' ></a>`})";

            IGrammar grammar = registry.LoadGrammar("source.ts");

            ITokenizeLineResult lineTokens = grammar.TokenizeLine(line);

            IToken[] tokens = lineTokens.Tokens;

            Assert.AreEqual(11, tokens.Length);

            AssertTokenValuesAreEqual(tokens[0],
                0, 1,
                "source.ts",
                "meta.decorator.ts",
                "punctuation.decorator.ts");
            AssertTokenValuesAreEqual(tokens[1],
                1, 10,
                "source.ts",
                "meta.decorator.ts",
                "entity.name.function.ts");
            AssertTokenValuesAreEqual(tokens[2],
                10, 11,
                "source.ts",
                "meta.decorator.ts",
                "meta.brace.round.ts");
            AssertTokenValuesAreEqual(tokens[3],
                11, 12,
                "source.ts",
                "meta.decorator.ts",
                "meta.object-literal.ts",
                "punctuation.definition.block.ts");
            AssertTokenValuesAreEqual(tokens[4],
                12, 20,
                "source.ts",
                "meta.decorator.ts",
                "meta.object-literal.ts",
                "meta.object.member.ts",
                "meta.object-literal.key.ts");
            AssertTokenValuesAreEqual(tokens[5],
                20, 21,
                "source.ts",
                "meta.decorator.ts",
                "meta.object-literal.ts",
                "meta.object.member.ts",
                "meta.object-literal.key.ts",
                "punctuation.separator.key-value.ts");
            AssertTokenValuesAreEqual(tokens[6],
                21, 22,
                "source.ts",
                "meta.decorator.ts",
                "meta.object-literal.ts",
                "meta.object.member.ts",
                "string.template.ts",
                "punctuation.definition.string.template.begin.ts");
            AssertTokenValuesAreEqual(tokens[7],
                22, 38,
                "source.ts",
                "meta.decorator.ts",
                "meta.object-literal.ts",
                "meta.object.member.ts",
                "string.template.ts");
            AssertTokenValuesAreEqual(tokens[8],
                38, 39,
                "source.ts",
                "meta.decorator.ts",
                "meta.object-literal.ts",
                "meta.object.member.ts",
                "string.template.ts",
                "punctuation.definition.string.template.end.ts");
            AssertTokenValuesAreEqual(tokens[9],
                39, 40,
                "source.ts",
                "meta.decorator.ts",
                "meta.object-literal.ts",
                "punctuation.definition.block.ts");
            AssertTokenValuesAreEqual(tokens[10],
                40, 41,
                "source.ts",
                "meta.decorator.ts",
                "meta.brace.round.ts");
        }

        static void AssertTokenValuesAreEqual(IToken token, int startIndex, int endIndex, params string[] scopes)
        {
            Assert.AreEqual(startIndex, token.StartIndex, "Unexpected token startIndex");
            Assert.AreEqual(endIndex, token.EndIndex, "Unexpected token endIndex");
            Assert.AreEqual(scopes.Length, token.Scopes.Count, "Unexpected scope lenght");

            for (int i = 0; i < scopes.Length; i++)
            {
                Assert.AreEqual(scopes[i], token.Scopes[i], "Unexpected scope at index " + i);
            }
        }

        static void AssertMetadataHasProperties(
            int metadata,
            int languageId,
            /*StandardTokenType*/ int tokenType,
            bool containsBalancedBrackets,
            int fontStyle,
            int foreground,
            int background)
        {
            string actual = "{\n" +
                "languageId: " + StackElementMetadata.GetLanguageId(metadata) + ",\n" +
                "tokenType: " + StackElementMetadata.GetTokenType(metadata) + ",\n" +
                "containsBalancedBrackets: " + StackElementMetadata.ContainsBalancedBrackets(metadata) + ",\n" +
                "fontStyle: " + StackElementMetadata.GetFontStyle(metadata) + ",\n" +
                "foreground: " + StackElementMetadata.GetForeground(metadata) + ",\n" +
                "background: " + StackElementMetadata.GetBackground(metadata) + ",\n" +
            "}";

            string expected = "{\n" +
                    "languageId: " + languageId + ",\n" +
                    "tokenType: " + tokenType + ",\n" +
                    "containsBalancedBrackets: " + containsBalancedBrackets + ",\n" +
                    "fontStyle: " + fontStyle + ",\n" +
                    "foreground: " + foreground + ",\n" +
                    "background: " + background + ",\n" +
                "}";

            Assert.AreEqual(expected, actual, "equals for " + StackElementMetadata.ToBinaryStr(metadata));
        }

        class TestRegistry : IRegistryOptions
        {
            public IRawTheme GetTheme(string scopeName)
            {
                using var stream = ResourceReader.OpenStream(GetFilePath(scopeName));
                using var reader = new StreamReader(stream);
                return ThemeReader.ReadThemeSync(reader);
            }

            public IRawGrammar GetGrammar(string scopeName)
            {
                using var stream = ResourceReader.OpenStream(GetFilePath(scopeName));
                using var reader = new StreamReader(stream);
                return GrammarReader.ReadGrammarSync(reader);
            }

            ICollection<string> IRegistryOptions.GetInjections(string scopeName)
            {
                return new List<string>() { "template.ng", "styles.ng" };
            }

            private string GetFilePath(string scopeName)
            {
                if ("source.batchfile".Equals(scopeName))
                {
                    return "batchfile.tmLanguage.json";
                }
                if ("source.css".Equals(scopeName))
                {
                    return "css.tmLanguage.json";
                }
                if ("source.cs".Equals(scopeName))
                {
                    return "csharp.tmLanguage.json";
                }
                if ("source.js".Equals(scopeName))
                {
                    return "JavaScript.tmLanguage.json";
                }
                else if ("text.html.basic".Equals(scopeName))
                {
                    return "html.json";
                }
                else if ("source.ts".Equals(scopeName))
                {
                    return "TypeScript.tmLanguage.json";
                }
                else if ("template.ng".Equals(scopeName))
                {
                    return "template.ng.json";
                }
                else if ("styles.ng".Equals(scopeName))
                {
                    return "styles.ng.json";
                }
                return null;
            }

            IRawTheme IRegistryOptions.GetDefaultTheme()
            {
                using (Stream stream = ResourceReader.OpenStream("dark_vs.json"))
                using (StreamReader reader = new StreamReader(stream))
                {
                    return ThemeReader.ReadThemeSync(reader);
                }
            }
        }
    }
}
