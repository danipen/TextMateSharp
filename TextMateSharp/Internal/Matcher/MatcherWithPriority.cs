using System;

namespace TextMateSharp.Internal.Matcher
{
    public class MatcherWithPriority<T>
    {
        public Predicate<T> matcher;
        public int priority;

        public MatcherWithPriority(Predicate<T> matcher, int priority)
        {
            this.matcher = matcher;
            this.priority = priority;
        }
    }
}