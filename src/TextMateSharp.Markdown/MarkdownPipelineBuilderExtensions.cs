using Markdig;
using TextMateSharp.Grammars;

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
		public MarkdownPipelineBuilder UseTextMateSharp(ThemeName? themeName = null)
		{
			var registryOptions = new RegistryOptions(themeName ?? ThemeName.DarkPlus);
			var registry = new Registry.Registry(registryOptions);
			return builder.UseTextMateSharp(registry, registryOptions);
		}

		/// <summary>
		/// Add TextMateSharp to handle code blocks.
		/// </summary>
		public MarkdownPipelineBuilder UseTextMateSharp(
			Registry.Registry registry,
			RegistryOptions registryOptions
		)
		{
			return builder.Use(new TextMateSharpExtension(registry, registryOptions));
		}
	}
}
