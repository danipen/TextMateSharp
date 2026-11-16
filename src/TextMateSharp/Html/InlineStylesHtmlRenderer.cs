using System;
using System.IO;
using System.Text.Encodings.Web;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace TextMateSharp.Html;

/// <summary>
/// Implementation of <see cref="IHtmlRenderer"/> that outputs tokens with inline styles.
/// </summary>
public class InlineStylesHtmlRenderer(Theme theme) : IHtmlRenderer
{
	/// <inheritdoc/>
	public void WriteHeader(TextWriter writer, string languageId)
	{
		var guiColors = theme.GetGuiColorDictionary();
		guiColors.TryGetValue("editor.background", out var backgroundColor);
		guiColors.TryGetValue("editor.foreground", out var foregroundColor);
		
		// `<pre><code class="language-...">` is the markup recommended in the HTML spec:
		// https://html.spec.whatwg.org/multipage/text-level-semantics.html#the-code-element
		writer.WriteLine();
		writer.Write("<pre style=\"");
		if (backgroundColor is not null)
		{
			writer.Write("background-color:");
			writer.Write(backgroundColor);
			writer.Write(';');
		}
		if (foregroundColor is not null)
		{
			writer.Write("color:");
			writer.Write(foregroundColor);
		}
		writer.Write("\"><code class=\"language-");
		HtmlEncoder.Default.Encode(writer, languageId);
		writer.Write("\">");
	}

	/// <inheritdoc/>
	public void WriteFooter(TextWriter writer)
	{
		writer.WriteLine("</code></pre>");
	}
	
	/// <inheritdoc/>
	public void Write(IToken token, ReadOnlyMemory<char> line, TextWriter writer)
	{
		var styles = ResolvedStyles.Create(token, theme);
		var isStyled = !styles.IsEmpty;
		if (isStyled)
		{
			writer.Write("<span style=\"");
			WriteStyles(styles, writer);
			writer.Write("\">");
		}
		writer.Write(token, line);
		if (isStyled)
		{
			writer.Write("</span>");
		}
	}

	/// <summary>
	/// Output the inline styles for the specified colours and font style.
	/// </summary>
	private void WriteStyles(
		ResolvedStyles styles,
		TextWriter writer
	)
	{
		var backgroundHex = ResolveColor(styles.BackgroundColor);
		var foregroundHex = ResolveColor(styles.ForegroundColor);
		
		if (foregroundHex is not null)
		{
			writer.Write("color:");
			writer.Write(foregroundHex);
			writer.Write(';');
		}

		if (backgroundHex is not null)
		{
			writer.Write("background-color:");
			writer.Write(backgroundHex);
			writer.Write(';');
		}

		if (styles.FontStyle is FontStyle.None or FontStyle.NotSet)
		{
			return;
		}

		if ((styles.FontStyle & FontStyle.Bold) != 0)
		{
			writer.Write("font-weight:bold;");
		}

		if ((styles.FontStyle & FontStyle.Italic) != 0)
		{
			writer.Write("font-style:italic;");
		}

		var isUnderlined = (styles.FontStyle & FontStyle.Underline) != 0;
		var isStrikethrough = (styles.FontStyle & FontStyle.Strikethrough) != 0;
		if (isUnderlined || isStrikethrough)
		{
			writer.Write("text-decoration:");
			if (isUnderlined)
			{
				writer.Write("underline ");
			}
			if (isStrikethrough)
			{
				writer.Write("line-through");
			}
			writer.Write(';');
		}
	}

	private string ResolveColor(int color)
	{
		return color == ResolvedStyles.NoColor ? null : theme.GetColor(color);
	}
}
