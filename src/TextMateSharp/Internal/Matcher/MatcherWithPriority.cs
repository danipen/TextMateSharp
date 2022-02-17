using System;

namespace TextMateSharp.Internal.Matcher
{
    public class MatcherWithPriority<T>
    {
        public Predicate<T> Matcher { get; private set; }
        public int Priority { get; private set; }

        public MatcherWithPriority(Predicate<T> matcher, int priority)
        {
            Matcher = matcher;
            Priority = priority;
        }
    }
}