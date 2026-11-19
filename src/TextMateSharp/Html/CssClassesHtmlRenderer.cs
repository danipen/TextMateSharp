using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using TextMateSharp.Grammars;

namespace TextMateSharp.Html;

/// <summary>
/// Implementation of <see cref="IHtmlRenderer"/> that outputs tokens with CSS class names.
/// </summary>
public class CssClassesHtmlRenderer : IHtmlRenderer
{
	/// <inheritdoc/>
	public void Write(
		TextWriter writer,
		string languageId,
		IEnumerable<(IToken[] Tokens, ReadOnlyMemory<char> Line)> lines
	)
	{
		WriteHeader(writer, languageId);
		WriteLines(writer, lines);
		WriteFooter(writer);
	}

	private void WriteHeader(TextWriter writer, string languageId)
	{
		// `<pre><code class="language-...">` is the markup recommended in the HTML spec:
		// https://html.spec.whatwg.org/multipage/text-level-semantics.html#the-code-element
		writer.WriteLine();
		writer.Write("<pre><code class=\"language-");
		HtmlEncoder.Default.Encode(writer, languageId);
		writer.Write("\">");
	}
	
	private void WriteFooter(TextWriter writer)
	{
		writer.WriteLine("</code></pre>");
	}

	private void WriteLines(
		TextWriter writer,
		IEnumerable<(IToken[] Tokens, ReadOnlyMemory<char> Line)> lines
	)
	{
		foreach (var (tokens, line) in lines)
		{
			foreach (var token in tokens)
			{
				writer.Write("<span class=\"");
				WriteScopes(writer, token.Scopes);
				writer.Write("\">");
				writer.Write(token, line);
				writer.Write("</span>");
			}
			writer.WriteLine();
		}
	}

	private void WriteScopes(
		TextWriter writer,
		IEnumerable<string> scopes
	)
	{
		var first = true;
		foreach (var scope in scopes)
		{
			if (!first)
			{
				writer.Write(' ');
			}
			first = false;
			WriteScopeClasses(writer, scope);
		}	
	}

	private void WriteScopeClasses(
		TextWriter writer,
		string scope
	)
	{
		// Scope looks like `string.quoted.double.cs`. We want to render each segment as a CSS 
		// class: `tm-string tm-string-quoted tm-string-quoted-double tm-string-quoted-double-cs`
		
		var length = scope.Length;
		
		Span<char> buffer = stackalloc char[length];
#if NETSTANDARD2_0
		scope.AsSpan().CopyTo(buffer);
#else
		// In newer .NET, we can avoid the intermediary .AsSpan()
		scope.CopyTo(buffer);
#endif

		var isFirst = true;
		for (var i = 0; i <= length; i++)
		{
			// We only need to output a class at the '.' separators, or at the end.
			var isEnd = i == length;
			var isDot = !isEnd && buffer[i] == '.';
			if (!isDot && !isEnd)
			{
				continue;	
			}

			// Replace dots with hyphens as we go
			if (isDot)
			{
				buffer[i] = '-';
			}

			if (!isFirst)
			{
				writer.Write(' ');
			}
			isFirst = false;
			
			writer.Write("tm-");
#if NETSTANDARD2_0
			// No ReadOnlySpan<char> overload in .NET Standard 2.0, so this is the best we can do.
			writer.Write(buffer[..i].ToArray());		
#else
			writer.Write(buffer[..i]);
#endif
		}
	}
}
