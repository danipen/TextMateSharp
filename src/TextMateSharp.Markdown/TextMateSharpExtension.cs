using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using TextMateSharp.Grammars;
using TextMateSharp.Html;

namespace TextMateSharp.Markdown;

public class TextMateSharpExtension(
	Registry.Registry registry,
	RegistryOptions registryOptions,
	IHtmlRenderer renderer = null
) : IMarkdownExtension
{
	private readonly IHtmlRenderer _renderer = 
		renderer ?? new InlineStylesHtmlRenderer(registry.GetTheme());

	/// <inheritdoc/>
	public void Setup(MarkdownPipelineBuilder pipeline)
	{
		// No-op: No parser changes needed.
	}

	/// <inheritdoc/>
	public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer markdownRenderer)
	{
		if (markdownRenderer is not HtmlRenderer markdownHtmlRenderer)
		{
			return;
		}
		
		// Find the existing render to use as a fallback, if any.
		var existingCodeBlock = markdownHtmlRenderer.ObjectRenderers.FindExact<CodeBlockRenderer>();
		if (existingCodeBlock is not null)
		{
			markdownHtmlRenderer.ObjectRenderers.Remove(existingCodeBlock);
		}
		else
		{
			// No existing renderer, so create a new one as the fallback.
			existingCodeBlock = new CodeBlockRenderer();
		}
		
		markdownHtmlRenderer.ObjectRenderers.AddIfNotAlready(
			new TextMateSharpCodeBlockRenderer(
				registry,
				registryOptions,
				existingCodeBlock,
				_renderer
			)
		);
	}
}
