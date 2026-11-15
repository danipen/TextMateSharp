using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using TextMateSharp.Html;
using TextMateSharp.Internal.Grammars;
using TextMateSharp.Internal.Themes;
using TextMateSharp.Themes;

namespace TextMateSharp.Tests.Html
{
    [TestFixture]
    public class InlineStylesTokenRendererTests
    {
        [TestCase("bold", "font-weight:bold;")]
        [TestCase("italic", "font-style:italic;")]
        [TestCase("underline", "text-decoration:underline ;")]
        [TestCase("strikethrough", "text-decoration:line-through;")]
        [TestCase("bold italic underline strikethrough",
            "font-weight:bold;font-style:italic;text-decoration:underline line-through;")]
        public void Write_StyledToken_WritesInlineStylesAndEscapesTokenText(
            string fontStyle, string expectedFontStyles)
        {
            var renderer = new InlineStylesTokenRenderer(CreateTheme(fontStyle));
            var token = new Token(7, 12, ["source.test"]);
            using var writer = new StringWriter();

            renderer.Write(token, "before <&\"> after", writer);

            Assert.AreEqual(
                "<span style=\"color:#123456;background-color:#ABCDEF;" +
                expectedFontStyles + "\">&lt;&amp;&quot;&gt; </span>",
                writer.ToString());
        }

        [Test]
        public void Write_TokenWithoutScopes_WritesEscapedTextWithoutSpan()
        {
            var renderer = new InlineStylesTokenRenderer(CreateTheme("bold"));
            var token = new Token(0, 4, []);
            using var writer = new StringWriter();

            renderer.Write(token, "<&\">", writer);

            Assert.AreEqual("&lt;&amp;&quot;&gt;", writer.ToString());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("none")]
        public void Write_ColorsWithoutFontStyle_DoesNotEmitFontStyles(string fontStyle)
        {
            var renderer = new InlineStylesTokenRenderer(CreateTheme(fontStyle));
            var token = new Token(0, 4, ["source.test"]);
            using var writer = new StringWriter();

            renderer.Write(token, "text", writer);

            Assert.AreEqual(
                "<span style=\"color:#123456;background-color:#ABCDEF;\">text</span>",
                writer.ToString());
        }

        [Test]
        public void Write_MultipleScopes_UsesInnerScopeStylesAndFallsBackToOuterScope()
        {
            var rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "source.test",
                        ["settings"] = new ThemeRaw
                        {
                            ["foreground"] = "#123456",
                            ["background"] = "#ABCDEF",
                            ["fontStyle"] = "bold"
                        }
                    },
                    new ThemeRaw
                    {
                        ["scope"] = "keyword.test",
                        ["settings"] = new ThemeRaw
                        {
                            ["foreground"] = "#FEDCBA",
                            ["fontStyle"] = "italic"
                        }
                    }
                }
            };
            var renderer = new InlineStylesTokenRenderer(Theme.CreateFromRawTheme(rawTheme, null));
            var token = new Token(0, 4, ["source.test", "keyword.test"]);
            using var writer = new StringWriter();

            renderer.Write(token, "text", writer);

            Assert.AreEqual(
                "<span style=\"color:#FEDCBA;background-color:#ABCDEF;font-style:italic;\">text</span>",
                writer.ToString());
        }

        [Test]
        public void Write_ConsecutiveTokens_DoesNotLeakStylesIntoUnstyledToken()
        {
            var renderer = new InlineStylesTokenRenderer(CreateTheme("bold"));
            using var writer = new StringWriter();

            renderer.Write(new Token(0, 1, ["source.test"]), "a&", writer);
            renderer.Write(new Token(1, 2, []), "a&", writer);

            Assert.AreEqual(
                "<span style=\"color:#123456;background-color:#ABCDEF;font-weight:bold;\">a</span>&amp;",
                writer.ToString());
        }

        [Test]
        public void Write_EmptyLine_WritesNothing()
        {
            var renderer = new InlineStylesTokenRenderer(CreateTheme(""));
            var token = new Token(0, 1, []);
            using var writer = new StringWriter();

            renderer.Write(token, "", writer);

            Assert.AreEqual("", writer.ToString());
        }

        [TestCase(1, 100, "bc")]
        [TestCase(100, 101, "")]
        [TestCase(1, 1, "")]
        public void Write_TokenBoundaries_ClampsToLineLength(int start, int end, string expected)
        {
            var renderer = new InlineStylesTokenRenderer(CreateTheme(""));
            var token = new Token(start, end, new List<string>());
            using var writer = new StringWriter();

            renderer.Write(token, "abc", writer);

            Assert.AreEqual(expected, writer.ToString());
        }

        private static Theme CreateTheme(string fontStyle)
        {
            var settings = new ThemeRaw
            {
                ["foreground"] = "#123456",
                ["background"] = "#ABCDEF"
            };
            if (fontStyle != null)
            {
                settings["fontStyle"] = fontStyle;
            }

            var rawTheme = new ThemeRaw
            {
                ["tokenColors"] = new List<IRawThemeSetting>
                {
                    new ThemeRaw
                    {
                        ["scope"] = "source.test",
                        ["settings"] = settings
                    }
                }
            };

            return Theme.CreateFromRawTheme(rawTheme, null);
        }
    }
}
