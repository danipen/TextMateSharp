using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Moq;
using NUnit.Framework;
using TextMateSharp.Grammars;
using TextMateSharp.Html;

namespace TextMateSharp.Markdown.Tests
{
    [TestFixture]
    public class TextMateSharpCodeBlockRendererTests
    {
        private MarkdownPipeline _pipeline;

        [OneTimeSetUp]
        public void SetUp()
        {
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseTextMateSharp(ThemeName.DarkPlus)
                .Build();
        }

        [TestCase("csharp", "Console.WriteLine(\"Hello world\" + 42);")]
        [TestCase("javascript", "const result = await loadSomeData();")]
        public void ToHtml_SupportedLanguage_HighlightsAndPreservesCode(string language, string source)
        {
            var html = Markdig.Markdown.ToHtml($"```{language}\n{source}\n```", _pipeline);
            var pre = XElement.Parse(html, LoadOptions.PreserveWhitespace);
            var code = pre.Element("code");

            Assert.AreEqual("language-" + language, (string)code.Attribute("class"));
            Assert.IsTrue(code.Elements("span").Any(), "Supported code should contain highlighted spans.");
            Assert.AreEqual(source + "\n", code.Value.Replace("\r\n", "\n"));
        }

        [Test]
        public void ToHtml_ConsecutiveStringTokens_MergesQuotesAndContent()
        {
            const string source = "Console.WriteLine(\"Hello world\" + 42);";

            var html = Markdig.Markdown.ToHtml("```csharp\n" + source + "\n```", _pipeline);
            var code = XElement.Parse(html, LoadOptions.PreserveWhitespace).Element("code");
            var literal = code.Elements("span").Single(span => span.Value.Contains("Hello world"));

            Assert.AreEqual("\"Hello world\"", literal.Value);
            Assert.AreEqual("color:#CE9178;", (string)literal.Attribute("style"));
        }

        [TestCase("var text = @\"first line\nsecond line\";", "@\"first line\nsecond line\"")]
        [TestCase("var text = @\"first line\n\nthird line\";", "@\"first line\n\nthird line\"")]
        [TestCase("/* first line\nsecond line */", "/* first line\nsecond line */\n")]
        public void ToHtml_IdenticalStylesAcrossLines_UsesOneSpan(string source, string expectedSpanText)
        {
            var html = Markdig.Markdown.ToHtml("```csharp\n" + source + "\n```", _pipeline);
            var code = XElement.Parse(html, LoadOptions.PreserveWhitespace).Element("code");
            var span = code.Elements("span").Single(element => element.Value.Contains("first line"));

            Assert.AreEqual(expectedSpanText, span.Value.Replace("\r\n", "\n"));
            Assert.AreEqual(source + "\n", code.Value.Replace("\r\n", "\n"));
        }

        [Test]
        public void ToHtml_EqualStylesSeparatedByOtherTokens_RemainSeparateSpans()
        {
            const string source = "var text = \"first\" + 42 + \"second\";";

            var html = Markdig.Markdown.ToHtml("```csharp\n" + source + "\n```", _pipeline);
            var code = XElement.Parse(html, LoadOptions.PreserveWhitespace).Element("code");
            var first = code.Elements("span").Single(span => span.Value.Contains("first"));
            var second = code.Elements("span").Single(span => span.Value.Contains("second"));

            Assert.AreEqual("\"first\"", first.Value);
            Assert.AreEqual("\"second\"", second.Value);
            Assert.AreEqual((string)first.Attribute("style"), (string)second.Attribute("style"));
        }

        [Test]
        public void ToHtml_ThemeGuiColors_AppliesEditorColorsToPre()
        {
            var options = new RegistryOptions(ThemeName.DarkPlus);
            var registry = new Registry.Registry(options);
            var pipeline = new MarkdownPipelineBuilder().UseTextMateSharp(options).Build();
            var colors = registry.GetTheme().GetGuiColorDictionary();

            var pre = XElement.Parse(Markdig.Markdown.ToHtml("```csharp\nvar x = 1;\n```", pipeline));

            Assert.AreEqual(
                "background-color:" + colors["editor.background"] + ";color:" + colors["editor.foreground"],
                (string)pre.Attribute("style"));
        }

        [TestCase("```\n<&>\n```")]
        [TestCase("```unknown-language\n<&>\n```")]
        [TestCase("    <&>\n")]
        public void ToHtml_UnsupportedCodeBlock_MatchesMarkdigFallback(string markdown)
        {
            var fallbackPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

            Assert.AreEqual(
                Markdig.Markdown.ToHtml(markdown, fallbackPipeline),
                Markdig.Markdown.ToHtml(markdown, _pipeline));
        }

        [Test]
        public void ToHtml_HtmlInCode_IsEscapedAndRemainsLiteralText()
        {
            const string source = "var html = \"<script>alert('x') & more</script>\";";

            var html = Markdig.Markdown.ToHtml("```csharp\n" + source + "\n```", _pipeline);
            var code = XElement.Parse(html, LoadOptions.PreserveWhitespace).Element("code");

            Assert.IsFalse(code.Descendants("script").Any());
            Assert.AreEqual(source + "\n", code.Value.Replace("\r\n", "\n"));
        }

        [TestCase("var first = 1;\n\nvar second = 2;")]
        [TestCase("  var indented = 1;\n    var deeper = 2;")]
        [TestCase("/* comment\nstill a comment */\nvar answer = 42;")]
        public void ToHtml_MultilineCode_PreservesLineBreaksAndWhitespace(string source)
        {
            var html = Markdig.Markdown.ToHtml("```csharp\n" + source + "\n```", _pipeline);

            Assert.AreEqual(
                source + "\n",
                XElement.Parse(html, LoadOptions.PreserveWhitespace).Element("code").Value.Replace("\r\n", "\n"));
        }

        [Test]
        public void ToHtml_MultipleCodeBlocks_PreservesSurroundingMarkdown()
        {
            const string markdown = "# Example\n\nBefore.\n\n```csharp\nvar x = 1;\n```\n\n" +
                "Between.\n\n```javascript\nconst y = 2;\n```\n\nAfter.";

            var html = Markdig.Markdown.ToHtml(markdown, _pipeline);
            var root = XElement.Parse("<root>" + html + "</root>");

            Assert.AreEqual("Example", root.Element("h1").Value);
            CollectionAssert.AreEqual(new[] { "Before.", "Between.", "After." }, root.Elements("p").Select(p => p.Value));
            CollectionAssert.AreEqual(
                new[] { "language-csharp", "language-javascript" },
                root.Elements("pre").Select(pre => (string)pre.Element("code").Attribute("class")));
        }

        [Test]
        public void ToHtml_MultilineComment_CarriesStateAcrossLinesButNotAcrossBlocks()
        {
            const string markdown = "```csharp\n/* first comment line\nstill a comment\n```\n\n" +
                "```csharp\nvar answer = 42;\n```";

            var html = Markdig.Markdown.ToHtml(markdown, _pipeline);
            var root = XElement.Parse("<root>" + html + "</root>");
            var blocks = root.Elements("pre").Select(pre => pre.Element("code")).ToArray();
            var firstLine = blocks[0].Elements("span").Single(span => span.Value.Contains("first comment line"));
            var continuation = blocks[0].Elements("span").Single(span => span.Value.Contains("still a comment"));
            var nextBlockKeyword = blocks[1].Elements("span").Single(span => span.Value == "var");

            Assert.AreSame(
                firstLine,
                continuation,
                "Consecutive comment lines should share a single span."
            );
            Assert.AreNotEqual((string)continuation.Attribute("style"), (string)nextBlockKeyword.Attribute("style"),
                "An unclosed comment must not affect the next code block.");
        }

        [Test]
        public void ToHtml_EmptySupportedFence_WritesEmptyCodeBlock()
        {
            var html = Markdig.Markdown.ToHtml("```csharp\n```", _pipeline);
            var code = XElement.Parse(html, LoadOptions.PreserveWhitespace).Element("code");

            Assert.AreEqual("language-csharp", (string)code.Attribute("class"));
            Assert.IsFalse(code.Elements().Any());
            Assert.AreEqual(string.Empty, code.Value);
        }

        [Test]
        public void ToHtml_TokenRendering_PassesOriginalStringAndLineSliceWithoutCopying()
        {
            const string source = "var answer = 42;";
            const string markdown = "```csharp\n" + source + "\n```";
            var options = new RegistryOptions(ThemeName.DarkPlus);
            var registry = new Registry.Registry(options);
            var tokenRenderer = new Mock<IHtmlRenderer>(MockBehavior.Strict);
            using var writer = new StringWriter();
            var renderer = new HtmlRenderer(writer);
            renderer.ObjectRenderers.Remove(renderer.ObjectRenderers.FindExact<CodeBlockRenderer>());
            renderer.ObjectRenderers.Add(new TextMateSharpCodeBlockRenderer(
                registry, options, new CodeBlockRenderer(), tokenRenderer.Object));
            tokenRenderer.Setup(r => r.Render(
                writer,
                "csharp",
                It.IsAny<IEnumerable<(IToken[] Tokens, ReadOnlyMemory<char> Line)>>())
            )
                .Callback<TextWriter, string, IEnumerable<(IToken[] Tokens, ReadOnlyMemory<char> Line)>>((output, languageId, lines) =>
                {
                    var line = lines.Single();
                    Assert.AreEqual(markdown.AsMemory(10, source.Length), line.Line);
                    Assert.IsNotEmpty(line.Tokens);
                    foreach (var token in line.Tokens)
                    {
                        output.Write(token, line.Line);
                    }
                    output.WriteLine();
                });

            renderer.Render(Markdig.Markdown.Parse(markdown));

            Assert.AreEqual(source + writer.NewLine, writer.ToString());
            tokenRenderer.Verify(r => r.Render(
                writer,
                "csharp",
                It.IsAny<IEnumerable<(IToken[] Tokens, ReadOnlyMemory<char> Line)>>()), Times.Once()
            );
        }

        [Test]
        public void ToHtml_ReusedPipeline_ProducesSameOutput()
        {
            const string markdown = "```csharp\nvar answer = 42;\n```\n\n```unknown-language\ntext\n```";

            var first = Markdig.Markdown.ToHtml(markdown, _pipeline);
            var second = Markdig.Markdown.ToHtml(markdown, _pipeline);

            Assert.AreEqual(first, second);
        }
    }
}
