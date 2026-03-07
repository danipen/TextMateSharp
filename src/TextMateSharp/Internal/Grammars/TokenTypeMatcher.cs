using System;
using System.Collections.Generic;

namespace TextMateSharp.Internal.Grammars
{
    internal sealed class TokenTypeMatcher
    {
        internal int Type { get; }
        internal Predicate<List<string>> Matcher { get; }

        internal TokenTypeMatcher(int type, Predicate<List<string>> matcher)
        {
            Type = type;
            Matcher = matcher;
        }
    }
}
