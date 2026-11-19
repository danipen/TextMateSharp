using System;
using System.Collections.Generic;
using System.IO;
using TextMateSharp.Grammars;

namespace TextMateSharp.Html;

public abstract class HtmlRendererBase : IHtmlRenderer
{
	/// <inheritdoc/>
	public abstract void Render(
		TextWriter writer,
		string languageId,
		IEnumerable<(IToken[] Tokens, ReadOnlyMemory<char> Line)> lines
	);

	/// <inheritdoc/>
	public string Render(
		string languageId,
		IEnumerable<(IToken[] Tokens, ReadOnlyMemory<char> Line)> lines
	)
	{
		using var writer = new StringWriter();
		Render(writer, languageId, lines);
		return writer.ToString();
	}
}