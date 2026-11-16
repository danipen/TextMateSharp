using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using TextMateSharp.Html;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars;
using TextMateSharp.Internal.Themes;
using TextMateSharp.Themes;

namespace TextMateSharp.Tests.Html
{
    [TestFixture]
    public class InlineStylesHtmlRendererTests
    {
        [TestCase("bold", "font-weight:bold;")]
        [TestCase("italic", "font-style:italic;")]
        [TestCase("underline", "text-decoration:underline ;")]
        [TestCase("strikethrough", "text-decoration:line-through;")]
        [TestCase("bold italic underline strikethrough",
            "font-weight:bold;font-style:italic;text-decoration:underline line-through;")]
        public void WriteLines_StyledToken_WritesInlineStylesAndEscapesTokenText(
            string fontStyle, string expectedFontStyles)
        {
            var renderer = new InlineStylesHtmlRenderer(CreateTheme(fontStyle));
            var token = new Token(7, 12, ["source.test"]);
            using var writer = new StringWriter();

            renderer.WriteLines(writer, [([token], "before <&\"> after".AsMemory())]);

            Assert.AreEqual(
                "<span style=\"color:#123456;background-color:#ABCDEF;" +
                expectedFontStyles + "\">&lt;&amp;&quot;&gt; " + writer.NewLine + "</span>",
                writer.ToString());
        }

        [Test]
        public void WriteLines_TokenWithoutScopes_WritesEscapedTextWithoutSpan()
        {
            var renderer = new InlineStylesHtmlRenderer(CreateTheme("bold"));
            var token = new Token(0, 4, []);
            using var writer = new StringWriter();

            renderer.WriteLines(writer, [([token], "<&\">".AsMemory())]);

            Assert.AreEqual("&lt;&amp;&quot;&gt;" + writer.NewLine, writer.ToString());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("none")]
        public void WriteLines_ColorsWithoutFontStyle_DoesNotEmitFontStyles(string fontStyle)
        {
            var renderer = new InlineStylesHtmlRenderer(CreateTheme(fontStyle));
            var token = new Token(0, 4, ["source.test"]);
            using var writer = new StringWriter();

            renderer.WriteLines(writer, [([token], "text".AsMemory())]);

            Assert.AreEqual(
                "<span style=\"color:#123456;background-color:#ABCDEF;\">text" + writer.NewLine + "</span>",
                writer.ToString());
        }

        [Test]
        public void WriteLines_MultipleScopes_UsesInnerScopeStylesAndFallsBackToOuterScope()
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
            var renderer = new InlineStylesHtmlRenderer(Theme.CreateFromRawTheme(rawTheme, null));
            var token = new Token(0, 4, ["source.test", "keyword.test"]);
            using var writer = new StringWriter();

            renderer.WriteLines(writer, [([token], "text".AsMemory())]);

            Assert.AreEqual(
                "<span style=\"color:#FEDCBA;background-color:#ABCDEF;font-style:italic;\">text" + writer.NewLine + "</span>",
                writer.ToString());
        }

        [Test]
        public void WriteLines_ConsecutiveTokens_DoesNotLeakStylesIntoUnstyledToken()
        {
            var renderer = new InlineStylesHtmlRenderer(CreateTheme("bold"));
            using var writer = new StringWriter();

            renderer.WriteLines(writer,
                [([new Token(0, 1, ["source.test"]), new Token(1, 2, [])], "a&".AsMemory())]);

            Assert.AreEqual(
                "<span style=\"color:#123456;background-color:#ABCDEF;font-weight:bold;\">a</span>&amp;" + writer.NewLine,
                writer.ToString());
        }

        [Test]
        public void WriteLines_EmptyLine_WritesLineBreak()
        {
            var renderer = new InlineStylesHtmlRenderer(CreateTheme(""));
            var token = new Token(0, 1, []);
            using var writer = new StringWriter();

            renderer.WriteLines(writer, [([token], ReadOnlyMemory<char>.Empty)]);

            Assert.AreEqual(writer.NewLine, writer.ToString());
        }

        [TestCase(1, 100, "bc")]
        [TestCase(100, 101, "")]
        [TestCase(1, 1, "")]
        public void WriteLines_TokenBoundaries_ClampsToLineLength(int start, int end, string expected)
        {
            var renderer = new InlineStylesHtmlRenderer(CreateTheme(""));
            var token = new Token(start, end, new List<string>());
            using var writer = new StringWriter();

            renderer.WriteLines(writer, [([token], "abc".AsMemory())]);

            Assert.AreEqual(expected + writer.NewLine, writer.ToString());
        }

        [TestCase(0, 4, "&lt;&amp;&quot;&gt;")]
        [TestCase(1, 3, "&amp;&quot;")]
        [TestCase(1, 100, "&amp;&quot;&gt;")]
        [TestCase(100, 101, "")]
        public void WriteLines_LineSlice_UsesRelativeTokenIndicesAndClampsToSliceLength(
            int start, int end, string expected)
        {
            IHtmlRenderer renderer = new InlineStylesHtmlRenderer(CreateTheme(""));
            var token = new Token(start, end, []);
            using var writer = new StringWriter();

            renderer.WriteLines(writer, [([token], "prefix <&\"> suffix".AsMemory(7, 4))]);

            Assert.AreEqual(expected + writer.NewLine, writer.ToString());
        }

        [Test]
        public void WriteLines_LineSliceToEnd_UsesRemainingString()
        {
            var renderer = new InlineStylesHtmlRenderer(CreateTheme(""));
            var token = new Token(0, 100, []);
            using var writer = new StringWriter();

            renderer.WriteLines(writer, [([token], "prefix <&".AsMemory(7))]);

            Assert.AreEqual("&lt;&amp;" + writer.NewLine, writer.ToString());
        }

        [Test]
        public void WriteLines_EmptyLineSlice_DoesNotWriteSurroundingText()
        {
            var renderer = new InlineStylesHtmlRenderer(CreateTheme(""));
            var token = new Token(0, 1, []);
            using var writer = new StringWriter();

            renderer.WriteLines(writer, [([token], "prefix suffix".AsMemory(7, 0))]);

            Assert.AreEqual(writer.NewLine, writer.ToString());
        }

        [Test]
        public void WriteLines_StyledLineSlice_WritesStylesAndEscapesOnlySlice()
        {
            var renderer = new InlineStylesHtmlRenderer(CreateTheme("bold"));
            var token = new Token(0, 100, ["source.test"]);
            using var writer = new StringWriter();

            renderer.WriteLines(writer, [([token], "prefix <& suffix".AsMemory(7, 2))]);

            Assert.AreEqual(
                "<span style=\"color:#123456;background-color:#ABCDEF;font-weight:bold;\">&lt;&amp;" + writer.NewLine + "</span>",
                writer.ToString());
        }

        [Test]
        public void WriteLines_ArrayBackedSlice_UsesRelativeIndicesAndEscapesText()
        {
            var renderer = new InlineStylesHtmlRenderer(CreateTheme(""));
            var token = new Token(1, 100, []);
            var source = "prefix <&\"> suffix".ToCharArray();
            using var writer = new StringWriter();

            renderer.WriteLines(writer, [([token], source.AsMemory(7, 4))]);

            Assert.AreEqual("&amp;&quot;&gt;" + writer.NewLine, writer.ToString());
        }

        [Test]
        public void WriteLines_ArrayBackedLongToken_EncodesAcrossBufferBoundaries()
        {
            var renderer = new InlineStylesHtmlRenderer(CreateTheme(""));
            var source = new string('<', 600) + "\U0001F600\uD800";
            var token = new Token(0, source.Length, []);
            using var writer = new StringWriter();

            renderer.WriteLines(writer, [([token], source.ToCharArray().AsMemory())]);

            Assert.AreEqual(HtmlEncoder.Default.Encode(source) + writer.NewLine, writer.ToString());
        }

        [Test]
        public void WriteLines_EqualStylesAcrossScopesAndBlankLines_MergesAndResetsBetweenCalls()
        {
            var renderer = new InlineStylesHtmlRenderer(CreateTheme("bold"));
            (IToken[] Tokens, ReadOnlyMemory<char> Line)[] lines =
            [
                ([new Token(0, 1, ["source.test"]), new Token(1, 2, ["source.test", "other.scope"])], "a<".AsMemory()),
                ([new Token(0, 1, [])], ReadOnlyMemory<char>.Empty),
                ([new Token(0, 1, ["source.test"])], "&".AsMemory())
            ];
            using var first = new StringWriter();
            using var second = new StringWriter();

            renderer.WriteLines(first, lines);
            renderer.WriteLines(second, lines);

            Assert.AreEqual(
                "<span style=\"color:#123456;background-color:#ABCDEF;font-weight:bold;\">a&lt;" +
                first.NewLine + first.NewLine + "&amp;" + first.NewLine + "</span>", first.ToString()
            );
            Assert.AreEqual(first.ToString(), second.ToString());
        }

        [Test]
        public void WriteLines_UnstyledToken_ClosesSpanAndKeepsLaterStyledTokenSeparate()
        {
            var renderer = new InlineStylesHtmlRenderer(CreateTheme("bold"));
            (IToken[] Tokens, ReadOnlyMemory<char> Line)[] lines =
            [
                ([new Token(0, 1, ["source.test"]), new Token(1, 2, []),
                    new Token(2, 3, ["source.test"])], "a&b".AsMemory())
            ];
            using var writer = new StringWriter();

            renderer.WriteLines(writer, lines);

            const string opening = "<span style=\"color:#123456;background-color:#ABCDEF;font-weight:bold;\">";
            Assert.AreEqual(
                opening + "a</span>&amp;" + opening + "b" + writer.NewLine + "</span>",
                writer.ToString()
            );
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
