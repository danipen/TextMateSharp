using TextMateSharp.Internal.Oniguruma;

using TextMateSharp.Internal.Rules;

namespace TextMateSharp.Internal.Matcher
{
    class MatchResult
    {
        internal OnigCaptureIndex[] CaptureIndexes { get; private set; }
        internal RuleId MatchedRuleId { get; private set; }

        internal MatchResult(OnigCaptureIndex[] captureIndexes, RuleId matchedRuleId)
        {
            CaptureIndexes = captureIndexes;
            MatchedRuleId = matchedRuleId;
        }
    }
}
