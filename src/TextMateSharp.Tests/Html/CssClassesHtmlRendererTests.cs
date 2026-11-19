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

            renderer.Write(writer, "test\"&", lines);

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
    }
}
