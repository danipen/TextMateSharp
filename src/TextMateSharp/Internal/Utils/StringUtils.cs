using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TextMateSharp.Internal.Utils
{
    internal static class StringUtils
    {
        private static Regex rrggbb = new Regex("^#[0-9a-f]{6}", RegexOptions.IgnoreCase);
        private static Regex rrggbbaa = new Regex("^#[0-9a-f]{8}", RegexOptions.IgnoreCase);
        private static Regex rgb = new Regex("^#[0-9a-f]{3}", RegexOptions.IgnoreCase);
        private static Regex rgba = new Regex("^#[0-9a-f]{4}", RegexOptions.IgnoreCase);

        internal static string SubstringAtIndexes(this string str, int startIndex, int endIndex)
        {
            return str.Substring(startIndex, endIndex - startIndex);
        }

        internal static bool IsValidHexColor(string hex)
        {
            if (hex == null || hex.Length < 1)
            {
                return false;
            }

            if (rrggbb.Match(hex).Success)
            {
                // #rrggbb
                return true;
            }

            if (rrggbbaa.Match(hex).Success)
            {
                // #rrggbbaa
                return true;
            }

            if (rgb.Match(hex).Success)
            {
                // #rgb
                return true;
            }

            if (rgba.Match(hex).Success)
            {
                // #rgba
                return true;
            }

            return false;
        }

        public static int StrCmp(string a, string b)
        {
            if (a == null && b == null)
            {
                return 0;
            }
            if (a == null)
            {
                return -1;
            }
            if (b == null)
            {
                return 1;
            }
            int result = a.CompareTo(b);
            if (result < 0)
            {
                return -1;
            }
            else if (result > 0)
            {
                return 1;
            }
            return 0;
        }

        public static int StrArrCmp(List<string> a, List<string> b)
        {
            if (a == null && b == null)
            {
                return 0;
            }
            if (a == null)
            {
                return -1;
            }
            if (b == null)
            {
                return 1;
            }
            int len1 = a.Count;
            int len2 = b.Count;
            if (len1 == len2)
            {
                for (int i = 0; i < len1; i++)
                {
                    int res = StrCmp(a[i], b[i]);
                    if (res != 0)
                    {
                        return res;
                    }
                }
                return 0;
            }
            return len1 - len2;
        }
    }
}
