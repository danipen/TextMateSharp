using Markdig;
using TextMateSharp.Grammars;
using TextMateSharp.Html;

namespace TextMateSharp.Markdown;

/// <summary>
/// Extension methods for <see cref="MarkdownPipelineBuilder"/>.
/// </summary>
public static class MarkdownPipelineBuilderExtensions
{
	extension(MarkdownPipelineBuilder builder)
	{
		/// <summary>
		/// Add TextMateSharp to handle code blocks.
		/// </summary>
		public MarkdownPipelineBuilder UseTextMateSharp(ThemeName themeName)
		{
			var registryOptions = new RegistryOptions(themeName);
			return builder.UseTextMateSharp(registryOptions);
		}

		/// <summary>
		/// Add TextMateSharp to handle code blocks.
		/// </summary>
		public MarkdownPipelineBuilder UseTextMateSharp(
			RegistryOptions registryOptions = null,
			Registry.Registry registry = null,
			IHtmlRenderer renderer = null
		)
		{
			registryOptions ??= new RegistryOptions(ThemeName.DarkPlus);
			registry ??= new Registry.Registry(registryOptions);
			return builder.Use(
				new TextMateSharpExtension(registry, registryOptions, renderer)
			);
		}
	}
}
