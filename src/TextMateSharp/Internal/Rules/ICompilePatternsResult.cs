using System.Collections.Generic;

namespace TextMateSharp.Internal.Rules
{
    public class CompilePatternsResult
    {
        public int?[] Patterns { get; private set; }
        public bool HasMissingPatterns { get; private set; }

        public CompilePatternsResult(IEnumerable<int?> patterns, bool hasMissingPatterns)
        {
            HasMissingPatterns = hasMissingPatterns;
            Patterns = new List<int?>(patterns).ToArray();
        }
    }
}