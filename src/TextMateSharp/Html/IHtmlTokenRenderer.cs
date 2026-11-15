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
	public void Write(IToken token, string line, TextWriter writer);
}