using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using TextMateSharp.Grammars;
using TextMateSharp.Html;

namespace TextMateSharp.Markdown;

public class TextMateSharpExtension(
	Registry.Registry registry,
	RegistryOptions registryOptions
) : IMarkdownExtension
{
	/// <inheritdoc/>
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		// No-op: No parser changes needed.
	}

	/// <inheritdoc/>
	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
	{
		if (renderer is not HtmlRenderer htmlRenderer)
		{
			return;
		}
		
		// Find the existing render to use as a fallback, if any.
		var existingRenderer = htmlRenderer.ObjectRenderers.FindExact<CodeBlockRenderer>();
		if (existingRenderer is not null)
		{
			htmlRenderer.ObjectRenderers.Remove(existingRenderer);
		}
		else
		{
			// No existing renderer, so create a new one as the fallback.
			existingRenderer = new CodeBlockRenderer();
		}

		var htmlTokenRenderer = new InlineStylesTokenRenderer(registry.GetTheme());
		htmlRenderer.ObjectRenderers.AddIfNotAlready(
			new TextMateSharpCodeBlockRenderer(
				registry,
				registryOptions,
				existingRenderer,
				htmlTokenRenderer
			)
		);
	}
}
