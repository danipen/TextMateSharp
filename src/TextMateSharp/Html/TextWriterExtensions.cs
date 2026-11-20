using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using TextMateSharp.Grammars;

namespace TextMateSharp.Html;

/// <summary>
/// Extension methods for <see cref="TextWriter"/>.
/// </summary>
public static class TextWriterExtensions
{
	extension(TextWriter writer)
	{
		/// <summary>
		/// Write the specified <see cref="IToken"/> to the <see cref="TextWriter"/>.
		/// </summary>
		public void Write(IToken token, ReadOnlyMemory<char> line)
		{
			var start = Math.Min(token.StartIndex, line.Length);
			var end = Math.Min(token.EndIndex, line.Length);
			var text = line.Slice(start, end - start);
			if (text.IsEmpty)
			{
				return;
			}

			// If the slice is backed by a string, we can output the string directly
			if (MemoryMarshal.TryGetString(text, out var source, out var offset, out var length))
			{
				HtmlEncoder.Default.Encode(writer, source, offset, length);
				return;
			}

#if NETSTANDARD2_0
			// .NET Standard 2.0 doesn't have the overload that takes ReadOnlyMemory<char>, so we'll
			// just fallback to the slower approach of allocating a string for the write.
			HtmlEncoder.Default.Encode(writer, text.ToString());
#else
			Span<char> buffer = stackalloc char[256];
			var remaining = text.Span;
			while (!remaining.IsEmpty)
			{
				HtmlEncoder.Default.Encode(remaining, buffer, out var consumed, out var written);
				writer.Write(buffer.Slice(0, written));
				remaining = remaining.Slice(consumed);
			}
#endif
		}
	}
}
