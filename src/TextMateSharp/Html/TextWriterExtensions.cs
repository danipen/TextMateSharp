using System;
using System.IO;
using System.Text.Encodings.Web;
using TextMateSharp.Grammars;

namespace TextMateSharp.Html;

/// <summary>
/// Extension methods for <see cref="TextWriter"/>.
/// </summary>
internal static class TextWriterExtensions
{
	/// <summary>
	/// Write the specified <see cref="IToken"/> to the <see cref="TextWriter"/>.
	/// </summary>
	public static void Write(this TextWriter writer, IToken token, string line)
	{
		var start = Math.Min(token.StartIndex, line.Length);
		var end = Math.Min(token.EndIndex, line.Length);
		HtmlEncoder.Default.Encode(writer, line, start, end - start);
	}
}