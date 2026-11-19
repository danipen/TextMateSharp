using System;
using System.Collections.Generic;
using System.IO;
using TextMateSharp.Grammars;

namespace TextMateSharp.Html;

/// <summary>
/// Represents a way to render tokens to HTML.
/// </summary>
public interface IHtmlRenderer
{
	/// <summary>
	/// Renders the specified tokenized code lines as HTML, to the specified
	/// <see cref="TextWriter"/>.
	/// </summary>
	public void Render(
		TextWriter writer,
		string languageId,
		IEnumerable<(IToken[] Tokens, ReadOnlyMemory<char> Line)> lines
	);
	
	/// <summary>
	/// Returns the specified tokenized code lines as HTML.
	/// </summary>
	public string Render(
		string languageId,
		IEnumerable<(IToken[] Tokens, ReadOnlyMemory<char> Line)> lines
	);
}
