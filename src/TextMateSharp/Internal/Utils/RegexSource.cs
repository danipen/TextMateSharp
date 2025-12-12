using System;
using System.Text;
using System.Text.RegularExpressions;
using Onigwrap;

namespace TextMateSharp.Internal.Utils
{
    public class RegexSource
    {

        private static Regex CAPTURING_REGEX_SOURCE = new Regex(
                "\\$(\\d+)|\\$\\{(\\d+):\\/(downcase|upcase)}");

        public static string EscapeRegExpCharacters(string value)
        {
            int valueLen = value.Length;
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
            return CAPTURING_REGEX_SOURCE.Match(regexSource).Success;
        }

        public static string ReplaceCaptures(string regexSource, ReadOnlyMemory<char> captureSource, IOnigCaptureIndex[] captureIndices)
        {
            return CAPTURING_REGEX_SOURCE.Replace(
                regexSource, m => GetReplacement(m.Value, captureSource, captureIndices));
        }

        private static string GetReplacement(string match, ReadOnlyMemory<char> captureSource, IOnigCaptureIndex[] captureIndices)
        {
            int index = -1;
            string command = null;
            int doublePointIndex = match.IndexOf(':');
            if (doublePointIndex != -1)
            {
                index = int.Parse(match.SubstringAtIndexes(2, doublePointIndex));
                command = match.SubstringAtIndexes(doublePointIndex + 2, match.Length - 1);
            }
            else
            {
                index = int.Parse(match.SubstringAtIndexes(1, match.Length));
            }
            IOnigCaptureIndex capture = captureIndices != null && captureIndices.Length > index ? captureIndices[index] : null;
            if (capture != null)
            {
                string result = captureSource.SubstringAtIndexes(capture.Start, capture.End);
                // Remove leading dots that would make the selector invalid
                while (result.Length > 0 && result[0] == '.')
                {
                    result = result.Substring(1);
                }
                if ("downcase".Equals(command))
                {
                    return result.ToLower();
                }
                else if ("upcase".Equals(command))
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
    }
}