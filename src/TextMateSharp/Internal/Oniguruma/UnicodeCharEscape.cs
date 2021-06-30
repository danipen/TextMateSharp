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
    }
}
