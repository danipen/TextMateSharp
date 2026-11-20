using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
	IHtmlRenderer htmlRenderer
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
		
		htmlRenderer.Render(renderer.Writer, languageId, lines: TokenizeLines(fenced, grammar));
	}

	private static IEnumerable<(IToken[] Tokens, ReadOnlyMemory<char> Line)> TokenizeLines(
		FencedCodeBlock fenced,
		IGrammar grammar
	)
	{
		IStateStack state = null;
		for (var i = 0; i < fenced.Lines.Count; i++)
		{
			var line = fenced.Lines.Lines[i];
			var slice = line.Slice;
			var lineText = slice.Text.AsMemory(slice.Start, slice.Length);
			var tokenized = grammar.TokenizeLine(lineText, state, TimeSpan.MaxValue);
			state = tokenized.RuleStack;
			yield return (tokenized.Tokens, lineText);
		}
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
