using System;
using System.Collections.Concurrent;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using TextMateSharp.Grammars;
using TextMateSharp.Html;

namespace TextMateSharp.Markdown;

/// <summary>
/// Code block renderer using TextMateSharp.
/// </summary>
public class TextMateSharpCodeBlockRenderer(
	Registry.Registry registry,
	RegistryOptions registryOptions,
	CodeBlockRenderer fallbackRenderer,
	IHtmlTokenRenderer htmlTokenRenderer
) : HtmlObjectRenderer<CodeBlock>
{
	private readonly ConcurrentDictionary<string, IGrammar> _grammarByLanguage = new();
	
	/// <inheritdoc/>
	protected override void Write(HtmlRenderer renderer, CodeBlock block)
	{
		// We only care about fenced code blocks.
		if (
			block is not FencedCodeBlock fenced ||
			fenced.Info is not { Length: > 0 } languageId ||
			!TryGetGrammar(languageId, out var grammar)
		)
		{
			fallbackRenderer.Write(renderer, block);
			return;
		}
		
		var theme = registry.GetTheme();
		var guiColors = theme.GetGuiColorDictionary();
		// TODO(daniel15) We only need to do this when using inline styles 
		guiColors.TryGetValue("editor.background", out var backgroundColor);
		guiColors.TryGetValue("editor.foreground", out var foregroundColor);

		renderer.EnsureLine();
		// `<pre><code class="language-...">` is the markup recommended in the HTML spec:
		// https://html.spec.whatwg.org/multipage/text-level-semantics.html#the-code-element
		// TODO(daniel15) This should be generalized and moved to TextMateSharp.Html namespace.
		renderer.Write("<pre style=\"");
		if (backgroundColor is not null)
		{
			renderer.Write("background-color:");
			renderer.Write(backgroundColor);
			renderer.Write(';');
		}
		if (foregroundColor is not null)
		{
			renderer.Write("color:");
			renderer.Write(foregroundColor);
		}
		renderer.Write("\"><code class=\"language-");
		renderer.WriteEscape(languageId);
		renderer.Write("\">");

		IStateStack state = null;
		for (var i = 0; i < fenced.Lines.Count; i++)
		{
			var line = fenced.Lines.Lines[i];
			var slice = line.Slice;
			var lineText = slice.Text.AsMemory(slice.Start, slice.Length);
			var tokenized = grammar.TokenizeLine(lineText, state, TimeSpan.MaxValue);
			state = tokenized.RuleStack;
		
			foreach (var token in tokenized.Tokens)
			{
				htmlTokenRenderer.Write(token, lineText, renderer.Writer);
			}
			renderer.WriteLine();
		}

		renderer.WriteLine("</code></pre>");
	}

	private bool TryGetGrammar(string languageId, out IGrammar grammar)
	{
		if (!_grammarByLanguage.TryGetValue(languageId, out grammar))
		{
			grammar = _grammarByLanguage.GetOrAdd(languageId, LoadGrammar);
		}
		return grammar != null;
	}

	private IGrammar LoadGrammar(string languageId)
	{
		var scope = registryOptions.GetScopeByLanguageId(languageId);
		return scope is null ? null : registry.LoadGrammar(scope);
	}
}
