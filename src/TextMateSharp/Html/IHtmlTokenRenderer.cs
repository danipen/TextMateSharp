using System;
using System.IO;
using TextMateSharp.Grammars;

namespace TextMateSharp.Html;

/// <summary>
/// Represents a way to render tokens to HTML.
/// </summary>
public interface IHtmlTokenRenderer
{
	/// <summary>
	/// Writes the token to the specified writer, including any required styling markup.
	/// </summary>
	/// <param name="token">A token with indices relative to the selected line slice.</param>
	/// <param name="line">The line slice containing the token.</param>
	/// <param name="writer">The output writer.</param>
	public void Write(IToken token, ReadOnlyMemory<char> line, TextWriter writer);
}
