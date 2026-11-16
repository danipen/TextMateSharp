using System;
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
	/// <param name="writer"></param>
	public void WriteFooter(TextWriter writer);
	
	/// <summary>
	/// Writes the token to the specified writer, including any required styling markup.
	/// </summary>
	/// <param name="token">A token with indices relative to the selected line slice.</param>
	/// <param name="line">The line slice containing the token.</param>
	/// <param name="writer">The output writer.</param>
	public void Write(IToken token, ReadOnlyMemory<char> line, TextWriter writer);
}
