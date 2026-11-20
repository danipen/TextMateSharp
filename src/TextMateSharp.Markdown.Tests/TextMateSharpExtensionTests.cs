using System.IO;
using System.Linq;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Moq;
using NUnit.Framework;
using TextMateSharp.Grammars;

namespace TextMateSharp.Markdown.Tests
{
    [TestFixture]
    public class TextMateSharpExtensionTests
    {
        [Test]
        public void Setup_HtmlRenderer_ReplacesCodeBlockRendererOnlyOnce()
        {
            var extension = CreateExtension();
            var pipeline = new MarkdownPipelineBuilder().Build();
            using var writer = new StringWriter();
            var renderer = new HtmlRenderer(writer);

            extension.Setup(pipeline, renderer);
            extension.Setup(pipeline, renderer);

            Assert.IsNull(renderer.ObjectRenderers.FindExact<CodeBlockRenderer>());
            Assert.AreEqual(1, renderer.ObjectRenderers.OfType<TextMateSharpCodeBlockRenderer>().Count());
            Assert.IsNotNull(renderer.ObjectRenderers.FindExact<ParagraphRenderer>());
        }

        [Test]
        public void Setup_NoExistingCodeBlockRenderer_CreatesWorkingFallback()
        {
            var pipeline = new MarkdownPipelineBuilder().Build();
            using var writer = new StringWriter();
            var renderer = new HtmlRenderer(writer);
            renderer.ObjectRenderers.Remove(renderer.ObjectRenderers.FindExact<CodeBlockRenderer>());
            CreateExtension().Setup(pipeline, renderer);

            renderer.Render(Markdig.Markdown.Parse("```\n<&>\n```", pipeline));

            Assert.AreEqual("<pre><code>&lt;&amp;&gt;\n</code></pre>\n", writer.ToString().Replace("\r\n", "\n"));
        }

        [Test]
        public void Setup_CustomFallbackRenderer_PreservesItsBehavior()
        {
            var pipeline = new MarkdownPipelineBuilder().Build();
            using var writer = new StringWriter();
            var renderer = new HtmlRenderer(writer);
            renderer.ObjectRenderers.Remove(renderer.ObjectRenderers.FindExact<CodeBlockRenderer>());
            var fallback = new CodeBlockRenderer { OutputAttributesOnPre = true };
            renderer.ObjectRenderers.Add(fallback);
            CreateExtension().Setup(pipeline, renderer);

            renderer.Render(Markdig.Markdown.Parse("```unknown-language\ntext\n```", pipeline));

            StringAssert.Contains("<pre class=\"language-unknown-language\"><code>", writer.ToString());
        }

        [Test]
        public void Setup_NonHtmlRenderer_DoesNotInteractWithRenderer()
        {
            var renderer = new Mock<IMarkdownRenderer>(MockBehavior.Strict);

            CreateExtension().Setup(new MarkdownPipelineBuilder().Build(), renderer.Object);

            renderer.VerifyNoOtherCalls();
        }

        private static TextMateSharpExtension CreateExtension()
        {
            var options = new RegistryOptions(ThemeName.DarkPlus);
            return new TextMateSharpExtension(new Registry.Registry(options), options);
        }
    }
}
