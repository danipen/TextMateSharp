using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using TextMateSharp.Tests.Resources;
using TextMateSharp.Themes;

namespace TextMateSharp.Tests.Internal.Grammars
{
    class GrammarTests
    {
        [Test]
        public void ParseSimpleTokensTest()
        {
            string line = "using System;";

            Registry.Registry registry = new Registry.Registry(
                new TestRegistry());

            IGrammar grammar = registry.LoadGrammar("source.cs");

            ITokenizeLineResult lineTokens = grammar.TokenizeLine(line);

            IToken[] tokens = lineTokens.GetTokens();

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
        public void GrammarInjectionTest()
        {
            Registry.Registry registry = new Registry.Registry(
                new TestRegistry());

            string line = "@Component({template:`<a href='' ></a>`})";

            IGrammar grammar = registry.LoadGrammar("source.ts");

            ITokenizeLineResult lineTokens = grammar.TokenizeLine(line);

            IToken[] tokens = lineTokens.GetTokens();

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

        class TestRegistry : IRegistryOptions
        {
            Stream IRegistryOptions.GetInputStream(string scopeName)
            {
                return ResourceReader.OpenStream(
                    ((IRegistryOptions)this).GetFilePath(scopeName));
            }

            ICollection<string> IRegistryOptions.GetInjections(string scopeName)
            {
                return new List<string>() { "template.ng", "styles.ng" };
            }

            string IRegistryOptions.GetFilePath(string scopeName)
            {
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

            IRawTheme IRegistryOptions.GetTheme()
            {
                return null;
            }
        }
    }
}
