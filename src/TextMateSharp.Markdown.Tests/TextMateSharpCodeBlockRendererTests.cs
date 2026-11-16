using System;
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
        public void ToHtml_ThemeGuiColors_AppliesEditorColorsToPre()
        {
            var options = new RegistryOptions(ThemeName.DarkPlus);
            var registry = new Registry.Registry(options);
            var pipeline = new MarkdownPipelineBuilder().UseTextMateSharp(registry, options).Build();
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

            Assert.AreEqual((string)firstLine.Attribute("style"), (string)continuation.Attribute("style"),
                "The second line should remain highlighted as a comment.");
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
            tokenRenderer.Setup(r => r.Write(
                It.IsAny<IToken>(), It.Is<ReadOnlyMemory<char>>(text => text.Equals(markdown.AsMemory(10, source.Length))),
                writer));

            renderer.Render(Markdig.Markdown.Parse(markdown));

            tokenRenderer.Verify(r => r.Write(
                It.IsAny<IToken>(), It.Is<ReadOnlyMemory<char>>(text => text.Equals(markdown.AsMemory(10, source.Length))),
                writer), Times.AtLeastOnce());
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
