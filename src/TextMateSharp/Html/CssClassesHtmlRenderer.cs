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
#if !NETSTANDARD2_0
		// Stack allocate a buffer to contain a string like "string-quoted-double"
		Span<char> buffer = stackalloc char[length];
		scope.CopyTo(buffer);
		for (var i = 0; i < buffer.Length; i++)
		{
			if (buffer[i] == '.')
			{
				buffer[i] = '-';
			}
		}
#else
		// Fall back to slower method in .NET Standard 2.0, since it doesn't support the
		// functionality we need.
		var buffer = scope.Replace('.', '-');
#endif
		
		var first = true;
		// For each `.` in the scope, render the contents of the buffer up until that point.
		// Given "string.quoted.double", this will first render "tm-string", then "tm-string-quoted",
		// then "tm-string-quoted-double".
		for (var i = 0; i <= length; i++)
		{
			if (i != scope.Length && scope[i] != '.')
				continue;

			if (!first)
			{
				writer.Write(' ');
			}
			first = false;

			writer.Write("tm-");
			writer.Write(buffer[..i]);
		}
	}
}
