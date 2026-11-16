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
	/// Writes the specified tokenized code lines as HTML.
	/// </summary>
	public void Write(
		TextWriter writer,
		string languageId,
		IEnumerable<(IToken[] Tokens, ReadOnlyMemory<char> Line)> lines
	);
}
