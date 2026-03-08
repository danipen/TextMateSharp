using Onigwrap;

using TextMateSharp.Internal.Rules;

namespace TextMateSharp.Internal.Matcher
{
    class MatchResult
    {
        internal IOnigCaptureIndex[] CaptureIndexes { get; private set; }
        internal RuleId MatchedRuleId { get; private set; }

        internal MatchResult(IOnigCaptureIndex[] captureIndexes, RuleId matchedRuleId)
        {
            CaptureIndexes = captureIndexes;
            MatchedRuleId = matchedRuleId;
        }
    }
}
