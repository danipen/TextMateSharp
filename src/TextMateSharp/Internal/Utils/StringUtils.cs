namespace TextMateSharp.Internal.Utils
{
    internal static class StringUtils
    {
        internal static string SubstringAtIndexes(this string str, int startIndex, int endIndex)
        {
            return str.Substring(startIndex, endIndex - startIndex);
        }
    }
}
