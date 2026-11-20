using NUnit.Framework;
using System;
using System.IO;
using TextMateSharp.Grammars;
using TextMateSharp.Html;
using TextMateSharp.Internal.Grammars;

namespace TextMateSharp.Tests.Html
{
    [TestFixture]
    public class CssClassesHtmlRendererTests
    {
        [Test]
        public void Write_MultipleScopesAndLines_WritesHierarchicalClassesAndEscapesHtml()
        {
            var renderer = new CssClassesHtmlRenderer();
            (IToken[] Tokens, ReadOnlyMemory<char> Line)[] lines =
            [
                ([
                    new Token(0, 1, ["source.test"]),
                    new Token(1, 4, ["source.test", "string.quoted.double.test"])
                ], "a<&\"".AsMemory()),
                ([], ReadOnlyMemory<char>.Empty),
                ([new Token(0, 1, [])], ">".AsMemory())
            ];
            using var writer = new StringWriter();

            renderer.Render(writer, "test\"&", lines);

            Assert.AreEqual(
                writer.NewLine + "<pre><code class=\"language-test&quot;&amp;\">" +
                "<span class=\"tm-source tm-source-test\">a</span>" +
                "<span class=\"tm-source tm-source-test tm-string tm-string-quoted " +
                "tm-string-quoted-double tm-string-quoted-double-test\">&lt;&amp;&quot;</span>" +
                writer.NewLine + writer.NewLine +
                "<span class=\"\">&gt;</span>" + writer.NewLine +
                "</code></pre>" + writer.NewLine,
                writer.ToString()
            );
        }
        [Test]
        public void Render_Allowlist_FiltersIndividualExpandedClassesWithoutMergingTokens()
        {
            var allowed = new[] { "string.quoted", "entity.name" };
            var renderer = new CssClassesHtmlRenderer(allowed);
            allowed[0] = "ignored";
            (IToken[] Tokens, ReadOnlyMemory<char> Line)[] lines =
            [
                ([
                    new Token(0, 1, ["source.test", "string.quoted.double", "entity.name.tag"]),
                    new Token(1, 2, ["string.quoted.single"]),
                    new Token(2, 3, ["string.quoted.single"]),
                    new Token(3, 4, ["stringy.quoted", "entity.other"])
                ], "<&ab".AsMemory())
            ];
            using var writer = new StringWriter();

            renderer.Render(writer, "test", lines);

            Assert.AreEqual(
                writer.NewLine + "<pre><code class=\"language-test\">" +
                "<span class=\"tm-string-quoted tm-entity-name\">&lt;</span>" +
                "<span class=\"tm-string-quoted\">&amp;</span>" +
                "<span class=\"tm-string-quoted\">a</span>" +
                "<span class=\"\">b</span>" + writer.NewLine +
                "</code></pre>" + writer.NewLine,
                writer.ToString());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Render_NullOrEmptyAllowlist_AllowsAllOrNoClasses(bool empty)
        {
            var renderer = new CssClassesHtmlRenderer(empty ? Array.Empty<string>() : null);
            (IToken[] Tokens, ReadOnlyMemory<char> Line)[] lines =
                [([new Token(0, 1, ["source.test"])], "a".AsMemory())];
            using var writer = new StringWriter();

            renderer.Render(writer, "test", lines);

            var classes = empty ? "" : "tm-source tm-source-test";
            Assert.AreEqual(
                writer.NewLine + "<pre><code class=\"language-test\"><span class=\"" + classes +
                "\">a</span>" + writer.NewLine + "</code></pre>" + writer.NewLine,
                writer.ToString());
        }
    }
}
