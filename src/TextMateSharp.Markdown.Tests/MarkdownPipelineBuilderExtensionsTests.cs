using Markdig;
using NUnit.Framework;
using TextMateSharp.Grammars;

namespace TextMateSharp.Markdown.Tests
{
    [TestFixture]
    public class MarkdownPipelineBuilderExtensionsTests
    {
        private const string Markdown = "```csharp\nvar answer = 42;\n```";

        [Test]
        public void UseTextMateSharp_ReturnsBuilderForChaining()
        {
            var builder = new MarkdownPipelineBuilder();

            var result = builder.UseTextMateSharp();

            Assert.AreSame(builder, result);
            Assert.AreEqual(1, builder.Extensions.Count);
            Assert.IsInstanceOf<TextMateSharpExtension>(builder.Extensions[0]);
        }

        [Test]
        public void UseTextMateSharp_DefaultTheme_IsDarkPlus()
        {
            var defaultPipeline = new MarkdownPipelineBuilder().UseTextMateSharp().Build();
            var darkPipeline = new MarkdownPipelineBuilder().UseTextMateSharp(ThemeName.DarkPlus).Build();

            Assert.AreEqual(
                Markdig.Markdown.ToHtml(Markdown, darkPipeline),
                Markdig.Markdown.ToHtml(Markdown, defaultPipeline));
        }

        [Test]
        public void UseTextMateSharp_SelectedTheme_ChangesHighlighting()
        {
            var darkPipeline = new MarkdownPipelineBuilder().UseTextMateSharp(ThemeName.DarkPlus).Build();
            var lightPipeline = new MarkdownPipelineBuilder().UseTextMateSharp(ThemeName.LightPlus).Build();

            Assert.AreNotEqual(
                Markdig.Markdown.ToHtml(Markdown, darkPipeline),
                Markdig.Markdown.ToHtml(Markdown, lightPipeline));
        }

        [Test]
        public void UseTextMateSharp_SuppliedRegistry_UsesItsTheme()
        {
            var options = new RegistryOptions(ThemeName.LightPlus);
            var registry = new Registry.Registry(options);
            var pipeline = new MarkdownPipelineBuilder().UseTextMateSharp(registry, options).Build();
            var expectedPipeline = new MarkdownPipelineBuilder().UseTextMateSharp(ThemeName.LightPlus).Build();

            Assert.AreEqual(
                Markdig.Markdown.ToHtml(Markdown, expectedPipeline),
                Markdig.Markdown.ToHtml(Markdown, pipeline));
        }
    }
}
