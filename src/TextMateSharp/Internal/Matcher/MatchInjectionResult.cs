using TextMateSharp.Internal.Oniguruma;
using TextMateSharp.Internal.Rules;

namespace TextMateSharp.Internal.Matcher
{
    internal class MatchInjectionsResult : MatchResult
    {
        public bool IsPriorityMatch { get; private set; }

        internal MatchInjectionsResult(
            IOnigCaptureIndex[] captureIndexes,
            RuleId matchedRuleId,
            bool isPriorityMatch) : base(captureIndexes, matchedRuleId)
        {
            IsPriorityMatch = isPriorityMatch;
        }
    }
}
