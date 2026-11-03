using System.Text;

namespace TextMateSharp.Internal.Utils
{
    // One StringBuilder per thread for short strings built while tokenizing (capture names, back-references).
    internal static class StringBuilderCache
    {
        private const int MaxCapacity = 512;

        [System.ThreadStatic]
        private static StringBuilder _cached;

        internal static StringBuilder Acquire(int capacity)
        {
            StringBuilder builder = _cached;
            if (builder != null && capacity <= builder.Capacity)
            {
                _cached = null;
                builder.Clear();
                return builder;
            }
            return new StringBuilder(capacity);
        }

        internal static string GetStringAndRelease(StringBuilder builder)
        {
            string result = builder.ToString();
            if (builder.Capacity <= MaxCapacity)
            {
                _cached = builder;
            }
            return result;
        }
    }
}
