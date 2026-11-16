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
	/// Writes the starting &lt;pre tag for an HTML code block.
	/// </summary>
	public void WriteHeader(TextWriter writer, string languageId);

	/// <summary>
	/// Writes the ending tag for an HTML code block;
	/// </summary>
	public void WriteFooter(TextWriter writer);
	
	/// <summary>
	/// Writes tokenized code lines, including styling markup.
	/// </summary>
	public void WriteLines(TextWriter writer, IEnumerable<(IToken[] Tokens, ReadOnlyMemory<char> Line)> lines);
}
