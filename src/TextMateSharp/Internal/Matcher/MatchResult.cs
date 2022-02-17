using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Internal.Matcher
{
    class MatchResult : IMatchResult
    {
        public IOnigCaptureIndex[] CaptureIndexes { get; private set; }
        public int? MatchedRuleId { get; private set; }

        internal MatchResult(IOnigCaptureIndex[] captureIndexes, int? matchedRuleId)
        {
            CaptureIndexes = captureIndexes;
            MatchedRuleId = matchedRuleId;
        }
    }
}
