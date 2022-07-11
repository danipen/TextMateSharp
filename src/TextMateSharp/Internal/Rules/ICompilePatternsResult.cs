using System.Collections.Generic;

namespace TextMateSharp.Internal.Rules
{
    public class CompilePatternsResult
    {
        public IList<RuleId> Patterns { get; private set; }
        public bool HasMissingPatterns { get; private set; }

        public CompilePatternsResult(IList<RuleId> patterns, bool hasMissingPatterns)
        {
            HasMissingPatterns = hasMissingPatterns;
            Patterns = patterns;
        }
    }
}