using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace TextMateSharp.Internal.Oniguruma
{
    public class UnicodeCharEscape
    {
        private static Regex UNICODE_WITHOUT_BRACES_PATTERN = new Regex("\\\\x[A-Fa-f0-9]{2,8}");

        public static string AddBracesToUnicodePatterns(string pattern)
        {
            return UNICODE_WITHOUT_BRACES_PATTERN.Replace(pattern, (m) =>
            {
                string prefix = "\\x";

                return string.Concat(
                    prefix,
                    "{", m.Value.Substring(prefix.Length), "}");
            });
        }

        internal static string ConstraintUnicodePatternLenght(string pattern)
        {
            // php grammar has this kind of unicode chars, and
            // oniguruma library doesn't like them
            return pattern.Replace("\\x{7fffffff}", "\\x{7ffff}");
        }
    }
}
