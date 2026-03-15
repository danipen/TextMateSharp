using System.Collections.Generic;

namespace TextMateSharp.Internal.Rules
{
    internal sealed class CompilePatternsResult
    {
        internal IList<RuleId> Patterns { get; private set; }
        internal bool HasMissingPatterns { get; private set; }

        internal CompilePatternsResult(IList<RuleId> patterns, bool hasMissingPatterns)
        {
            HasMissingPatterns = hasMissingPatterns;
            Patterns = patterns;
        }
    }
}