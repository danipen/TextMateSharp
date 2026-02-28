using System;
using System.Collections.Generic;

namespace TextMateSharp.Internal.Utils
{
    internal static class StringUtils
    {
        internal static string SubstringAtIndexes(this string str, int startIndex, int endIndex)
        {
            if (startIndex < 0) throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (endIndex > str.Length) throw new ArgumentOutOfRangeException(nameof(endIndex));
            if (startIndex > endIndex) throw new ArgumentOutOfRangeException(nameof(startIndex));

            return str.AsSpan(startIndex, endIndex - startIndex).ToString();
        }

        internal static ReadOnlyMemory<char> SliceAtIndexes(this ReadOnlyMemory<char> memory, int startIndex, int endIndex)
        {
            if (startIndex < 0) throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (endIndex > memory.Length) throw new ArgumentOutOfRangeException(nameof(endIndex));
            if (startIndex > endIndex) throw new ArgumentOutOfRangeException(nameof(startIndex));

            return memory.Slice(startIndex, endIndex - startIndex);
        }

        internal static ReadOnlySpan<char> SliceAtIndexes(this ReadOnlySpan<char> span, int startIndex, int endIndex)
        {
            if (startIndex < 0) throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (endIndex > span.Length) throw new ArgumentOutOfRangeException(nameof(endIndex));
            if (startIndex > endIndex) throw new ArgumentOutOfRangeException(nameof(startIndex));

            return span.Slice(startIndex, endIndex - startIndex);
        }

        internal static string SubstringAtIndexes(this ReadOnlyMemory<char> memory, int startIndex, int endIndex)
        {
            if (startIndex < 0) throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (endIndex > memory.Length) throw new ArgumentOutOfRangeException(nameof(endIndex));
            if (startIndex > endIndex) throw new ArgumentOutOfRangeException(nameof(startIndex));

            return memory.Slice(startIndex, endIndex - startIndex).Span.ToString();
        }

        /// <summary>
        /// Determines whether the specified string represents a valid hexadecimal color value.
        /// </summary>
        /// <remarks>Valid hexadecimal color values can be specified in shorthand (#rgb, #rgba) or full
        /// (#rrggbb, #rrggbbaa) formats. The method checks for the presence of valid hexadecimal digits in the
        /// appropriate positions based on the length of the input string.</remarks>
        /// <param name="hex">The hexadecimal color string to validate. The string must begin with a '#' character and may be in the
        /// formats #rgb, #rgba, #rrggbb, or #rrggbbaa.</param>
        /// <returns>true if the specified string is a valid hexadecimal color; otherwise, false.</returns>
        internal static bool IsValidHexColor(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex[0] != '#')
            {
                return false;
            }

            // Keep the same precedence as the original regex checks.
            if (hex.Length >= 7 && HasHexDigits(hex, 1, 6))
            {
                // #rrggbb
                return true;
            }

            if (hex.Length >= 9 && HasHexDigits(hex, 1, 8))
            {
                // #rrggbbaa
                return true;
            }

            if (hex.Length >= 4 && HasHexDigits(hex, 1, 3))
            {
                // #rgb
                return true;
            }

            if (hex.Length >= 5 && HasHexDigits(hex, 1, 4))
            {
                // #rgba
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether a specified substring of a hexadecimal string contains only valid hexadecimal digits.
        /// </summary>
        /// <remarks>Valid hexadecimal digits are 0-9, A-F, and a-f. The method does not validate the
        /// bounds of the substring; callers should ensure that startIndex and count specify a valid range within the
        /// string.</remarks>
        /// <param name="hex">The string to evaluate for valid hexadecimal digits.</param>
        /// <param name="startIndex">The zero-based index at which to begin evaluating the substring within the hexadecimal string.</param>
        /// <param name="count">The number of characters to evaluate from the starting index.</param>
        /// <returns>true if all characters in the specified substring are valid hexadecimal digits; otherwise, false.</returns>
        private static bool HasHexDigits(string hex, int startIndex, int count)
        {
            int end = startIndex + count;
            for (int i = startIndex; i < end; i++)
            {
                if (!IsHexDigit(hex[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether the specified character represents a valid hexadecimal digit.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method performs a case-insensitive check for hexadecimal digits, allowing both uppercase and lowercase letters.</para>
        /// <para>
        /// Implements a fast hex-digit check without allocations. The expressions:
        ///	    1. <c>(uint) (c - '0') &lt;= 9</c> is true for '0' to '9'
        ///	    2. <c>(uint) ((c | 0x20) - 'a') &lt;= 5</c> lowercases ASCII letters by setting bit 0x20, then checks 'a' to 'f'
        ///     3. using uint avoids branching on negative values and keeps the comparisons simple
        /// </para>
        /// </remarks>
        /// <param name="c">The character to evaluate as a hexadecimal digit.</param>
        /// <returns>true if the character is a valid hexadecimal digit (0-9, A-F, or a-f); otherwise, false.</returns>
        private static bool IsHexDigit(char c)
        {
            return (uint)(c - '0') <= 9 || (uint)((c | 0x20) - 'a') <= 5;
        }

        public static int StrCmp(string a, string b)
        {
            if (a == b)
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
            if (a == b)
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