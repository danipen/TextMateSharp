using System;
using System.Linq;

using NUnit.Framework;

namespace TextMateSharp.Grammars.Tests
{
    [TestFixture]
    public class CssGrammarTests
    {
        private IGrammar _grammar;

        [SetUp]
        public void SetUp()
        {
            var options = new RegistryOptions(ThemeName.DarkPlus);
            var registry = new Registry.Registry(options);
            _grammar = registry.LoadGrammar(options.GetScopeByLanguageId("css"));
            Assert.That(_grammar, Is.Not.Null);
        }

        [TestCase("body { margin: 25px; }", "margin", "support.type.property-name.css")]
        [TestCase("body { margin: 25px; }", "px", "keyword.other.unit.px.css")]
        [TestCase("a { color: #1234abcd; }", "#1234abcd", "constant.other.color.rgb-value.hex.css")]
        [TestCase("a { content: \"hello\"; }", "hello", "string.quoted.double.css")]
        [TestCase("/* comment */", "comment", "comment.block.css")]
        [TestCase("@layer reset, theme;", "@layer", "keyword.control.at-rule.layer.css")]
        [TestCase("@layer reset, theme;", "theme", "variable.parameter.layer-name.css")]
        [TestCase("@import url('theme.css') layer(theme);", "layer", "support.function.layer.css")]
        [TestCase(":is(.card, #main) { color: red; }", ".card", "entity.other.attribute-name.class.css")]
        [TestCase(":where(.card, #main) { color: red; }", "#main", "entity.other.attribute-name.id.css")]
        [TestCase("a { color: oklch(60% 0.2 30); }", "oklch", "support.function.misc.css")]
        [TestCase("a { color: color-mix(in srgb, red, blue); }", "color-mix", "support.function.misc.css")]
        [TestCase("a { width: calc((100% - 2rem) / 2); }", "/", "keyword.operator.arithmetic.css")]
        [TestCase("a { width: sin(1rad); }", "sin", "support.function.misc.css")]
        [TestCase("a { width: --spacing(2); }", "--spacing", "support.function.custom.css")]
        [TestCase("a { height: 100dvh; }", "dvh", "keyword.other.unit.dvh.css")]
        [TestCase("a { width: 50cqi; }", "cqi", "keyword.other.unit.cqi.css")]
        [TestCase("a { container-type: inline-size; }", "container-type", "support.type.property-name.css")]
        [TestCase("a { aspect-ratio: 16 / 9; }", "aspect-ratio", "support.type.property-name.css")]
        [TestCase("a { color: revert-layer; }", "revert-layer", "support.constant.property-value.css")]
        [TestCase("@media (width >= calc(40rem + 1px)) { }", "calc", "support.function.calc.css")]
        public void Tokenizes_Css_With_Expected_Scope(string line, string text, string scope)
        {
            AssertScope(_grammar.TokenizeLine(line), line, text, scope);
        }

        [Test]
        public void Preserves_State_Through_Layers_And_Nested_Rules()
        {
            string[] lines =
            {
                "@layer components {",
                "  .card {",
                "    &:hover {",
                "      color: var(--accent, oklch(60% 0.2 30));",
                "    }",
                "    display: grid;",
                "  }",
                "}",
                "body { margin: 0; }"
            };

            IStateStack state = null;
            ITokenizeLineResult[] results = new ITokenizeLineResult[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                results[i] = _grammar.TokenizeLine(lines[i], state, TimeSpan.MaxValue);
                state = results[i].RuleStack;
            }

            AssertScope(results[2], lines[2], "&", "entity.name.tag.nesting.css");
            AssertScope(results[2], lines[2], ":hover", "entity.other.attribute-name.pseudo-class.css");
            AssertScope(results[3], lines[3], "--accent", "variable.argument.css");
            AssertScope(results[3], lines[3], "oklch", "meta.at-rule.layer.body.css");
            AssertScope(results[5], lines[5], "display", "support.type.property-name.css");
            AssertScope(results[8], lines[8], "body", "entity.name.tag.css");
            Assert.That(results[8].Tokens.SelectMany(token => token.Scopes),
                Does.Not.Contain("meta.at-rule.layer.body.css"));
        }

        private static void AssertScope(ITokenizeLineResult result, string line, string text, string scope)
        {
            int start = line.IndexOf(text, StringComparison.Ordinal);
            Assert.That(start, Is.GreaterThanOrEqualTo(0));
            for (int i = start; i < start + text.Length; i++)
            {
                var token = result.Tokens.Single(t => t.StartIndex <= i && i < t.EndIndex);
                Assert.That(token.Scopes, Does.Contain(scope), $"Scope of '{text}' at offset {i} in '{line}'");
            }
        }
    }
}
