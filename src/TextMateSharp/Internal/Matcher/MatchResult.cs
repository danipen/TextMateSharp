using TextMateSharp.Internal.Oniguruma;
using TextMateSharp.Internal.Rules;

namespace TextMateSharp.Internal.Matcher
{
    class MatchResult : IMatchResult
    {
        public IOnigCaptureIndex[] CaptureIndexes { get; private set; }
        public RuleId MatchedRuleId { get; private set; }

        internal MatchResult(IOnigCaptureIndex[] captureIndexes, RuleId matchedRuleId)
        {
            CaptureIndexes = captureIndexes;
            MatchedRuleId = matchedRuleId;
        }
    }
}
