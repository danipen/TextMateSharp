using System.Collections.Generic;

namespace TextMateSharp.Internal.Rules
{
    public class ICompilePatternsResult
    {
        public int?[] patterns;
        public bool hasMissingPatterns;

        public ICompilePatternsResult(IEnumerable<int?> patterns, bool hasMissingPatterns)
        {
            this.hasMissingPatterns = hasMissingPatterns;
            this.patterns = new List<int?>(patterns).ToArray();
        }
    }
}