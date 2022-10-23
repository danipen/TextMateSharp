using System;
using System.Collections.Generic;

namespace TextMateSharp.Internal.Grammars
{
    internal class TokenTypeMatcher
    {
        public int Type { get; }
        public Predicate<List<string>> Matcher { get; }

        public TokenTypeMatcher(int type, Predicate<List<string>> matcher)
        {
            Type = type;
            Matcher = matcher;
        }
    }
}
