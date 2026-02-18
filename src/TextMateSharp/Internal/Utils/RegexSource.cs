using Onigwrap;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace TextMateSharp.Internal.Utils
{
    public class RegexSource
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

        public static string ReplaceCaptures(string regexSource, ReadOnlyMemory<char> captureSource, IOnigCaptureIndex[] captureIndices)
        {
            return CAPTURING_REGEX_SOURCE.Replace(
                regexSource, m => GetReplacement(m.Value, captureSource, captureIndices));
        }

        private static string GetReplacement(string match, ReadOnlyMemory<char> captureSource, IOnigCaptureIndex[] captureIndices)
        {
            ReadOnlySpan<char> matchSpan = match.AsSpan();
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

            IOnigCaptureIndex capture = captureIndices != null && captureIndices.Length > index ? captureIndices[index] : null;
            if (capture != null)
            {
                string result = captureSource.SubstringAtIndexes(capture.Start, capture.End);

                // Remove leading dots that would make the selector invalid
                int start = 0;
                while (start < result.Length && result[start] == '.')
                {
                    start++;
                }
                if (start != 0)
                {
                    result = result.Substring(start);
                }
                if (commandSpan.SequenceEqual("downcase"))
                {
                    return result.ToLower();
                }
                else if (commandSpan.SequenceEqual("upcase"))
                {
                    return result.ToUpper();
                }
                else
                {
                    return result;
                }
            }
            else
            {
                return match;
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