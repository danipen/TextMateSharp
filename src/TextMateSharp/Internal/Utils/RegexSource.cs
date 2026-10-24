using TextMateSharp.Internal.Oniguruma;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace TextMateSharp.Internal.Utils
{
    public static class RegexSource
    {

        private static readonly Regex CAPTURING_REGEX_SOURCE = new Regex(
                "\\$(\\d+)|\\$\\{(\\d+):\\/(downcase|upcase)}");

        public static string EscapeRegExpCharacters(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            int valueLen = value.Length;
            if (valueLen == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder(valueLen);
            for (int i = 0; i < valueLen; i++)
            {
                char ch = value[i];
                switch (ch)
                {
                    case '-':
                    case '\\':
                    case '{':
                    case '}':
                    case '*':
                    case '+':
                    case '?':
                    case '|':
                    case '^':
                    case '$':
                    case '.':
                    case ',':
                    case '[':
                    case ']':
                    case '(':
                    case ')':
                    case '#':
                        /* escaping white space chars is actually not necessary:
                        case ' ':
                        case '\t':
                        case '\n':
                        case '\f':
                        case '\r':
                        case 0x0B: // vertical tab \v
                        */
                        sb.Append('\\');
                        break;
                }
                sb.Append(ch);
            }
            return sb.ToString();
        }

        public static bool HasCaptures(string regexSource)
        {
            if (regexSource == null)
            {
                return false;
            }
            return CAPTURING_REGEX_SOURCE.IsMatch(regexSource);
        }

        public static string ReplaceCaptures(string regexSource, ReadOnlyMemory<char> captureSource, OnigCaptureIndex[] captureIndices)
        {
            // CAPTURING_REGEX_SOURCE.Replace, without the regex: each "$n" or "${n:/downcase}" / "${n:/upcase}",
            // scanning from left to right, is replaced by its capture.
            if (regexSource == null) throw new ArgumentNullException(nameof(regexSource));

            int dollar = regexSource.IndexOf('$');
            if (dollar < 0)
            {
                return regexSource;
            }

            StringBuilder builder = null;
            int copied = 0;
            for (int i = dollar; i < regexSource.Length; i++)
            {
                if (regexSource[i] != '$')
                {
                    continue;
                }

                int end = MatchCapture(regexSource, i);
                if (end < 0)
                {
                    continue;
                }

                builder ??= StringBuilderCache.Acquire(regexSource.Length + 16);
                builder.Append(regexSource, copied, i - copied);
                AppendReplacement(builder, regexSource.AsSpan(i, end - i), captureSource, captureIndices);
                copied = end;
                i = end - 1;
            }

            if (builder == null)
            {
                return regexSource;
            }

            builder.Append(regexSource, copied, regexSource.Length - copied);
            return StringBuilderCache.GetStringAndRelease(builder);
        }

        // The end of the CAPTURING_REGEX_SOURCE match at the '$' at start, or -1.
        private static int MatchCapture(string source, int start)
        {
            int pos = start + 1;
            int digits = CountDigits(source, pos);
            if (digits > 0)
            {
                // \$(\d+)
                return pos + digits;
            }

            // \$\{(\d+):\/(downcase|upcase)}
            if (pos >= source.Length || source[pos] != '{')
            {
                return -1;
            }
            pos++;
            digits = CountDigits(source, pos);
            if (digits == 0)
            {
                return -1;
            }
            pos += digits;
            if (pos + 1 >= source.Length || source[pos] != ':' || source[pos + 1] != '/')
            {
                return -1;
            }
            pos += 2;
            if (string.CompareOrdinal(source, pos, "downcase}", 0, 9) == 0)
            {
                return pos + 9;
            }
            if (string.CompareOrdinal(source, pos, "upcase}", 0, 7) == 0)
            {
                return pos + 7;
            }
            return -1;
        }

        // The number of digits (\d, any Unicode decimal digit) at start.
        internal static int CountDigits(string source, int start)
        {
            int end = start;
            while (end < source.Length && char.IsDigit(source[end]))
            {
                end++;
            }
            return end - start;
        }

        private static void AppendReplacement(StringBuilder builder, ReadOnlySpan<char> matchSpan, ReadOnlyMemory<char> captureSource, OnigCaptureIndex[] captureIndices)
        {
            int doublePointIndex = matchSpan.IndexOf(':');

            int index = ParseCaptureIndex(matchSpan, doublePointIndex);

            ReadOnlySpan<char> commandSpan = default;
            if (doublePointIndex != -1)
            {
                int commandStart = doublePointIndex + 2;
                int commandLength = matchSpan.Length - commandStart - 1; // exclude trailing '}'
                if (commandLength > 0)
                {
                    commandSpan = matchSpan.Slice(commandStart, commandLength);
                }
            }

            OnigCaptureIndex capture = captureIndices != null && captureIndices.Length > index ? captureIndices[index] : default;
            if (!capture.IsDefined)
            {
                AppendSpan(builder, matchSpan);
                return;
            }

            if (capture.Start < 0) throw new ArgumentOutOfRangeException(nameof(captureIndices));
            if (capture.End > captureSource.Length) throw new ArgumentOutOfRangeException(nameof(captureIndices));
            if (capture.Start > capture.End) throw new ArgumentOutOfRangeException(nameof(captureIndices));

            // Remove leading dots that would make the selector invalid
            int start = capture.Start;
            while (start < capture.End && captureSource.Span[start] == '.')
            {
                start++;
            }
            ReadOnlyMemory<char> result = captureSource.Slice(start, capture.End - start);

            if (commandSpan.SequenceEqual("downcase".AsSpan()))
            {
                builder.Append(result.Span.ToString().ToLower());
            }
            else if (commandSpan.SequenceEqual("upcase".AsSpan()))
            {
                builder.Append(result.Span.ToString().ToUpper());
            }
            else if (MemoryMarshal.TryGetString(result, out string text, out int textStart, out int textLength))
            {
                builder.Append(text, textStart, textLength);
            }
            else
            {
                AppendSpan(builder, result.Span);
            }
        }

        private static void AppendSpan(StringBuilder builder, ReadOnlySpan<char> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                builder.Append(span[i]);
            }
        }

        private static int ParseCaptureIndex(ReadOnlySpan<char> matchSpan, int doublePointIndex)
        {
            int start = doublePointIndex != -1 ? 2 : 1;
            int end = doublePointIndex != -1 ? doublePointIndex : matchSpan.Length;

            int value = 0;
            for (int i = start; i < end; i++)
            {
                value = (value * 10) + (matchSpan[i] - '0');
            }

            return value;
        }
    }
}