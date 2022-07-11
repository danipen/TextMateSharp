using TextMateSharp.Internal.Oniguruma;
using TextMateSharp.Internal.Rules;

namespace TextMateSharp.Internal.Matcher
{
    internal class MatchInjectionsResult : IMatchInjectionsResult
    {
        public IOnigCaptureIndex[] CaptureIndexes { get; private set; }
        public RuleId MatchedRuleId { get; private set; }
        public bool IsPriorityMatch { get; private set; }

        internal MatchInjectionsResult(
            IOnigCaptureIndex[] captureIndexes,
            RuleId matchedRuleId,
            bool isPriorityMatch)
        {
            CaptureIndexes = captureIndexes;
            MatchedRuleId = matchedRuleId;
            IsPriorityMatch = isPriorityMatch;
        }
    }
}
