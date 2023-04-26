using System;
using System.Collections.Generic;
using System.Text;

namespace TextMateSharp.Internal.Matcher
{
    internal class Matcher
    {
        internal static ICollection<MatcherWithPriority<List<string>>> CreateMatchers(string selector)
        {
            return CreateMatchers(selector, NameMatcher.Default);
        }

        public static List<MatcherWithPriority<List<string>>> CreateMatchers(
            string selector, IMatchesName<List<string>> matchesName)
        {
            return new MatcherBuilder<List<string>> (selector, matchesName).Results;
        }
    }
}
