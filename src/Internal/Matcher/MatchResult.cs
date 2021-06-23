using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Internal.Matcher
{
    class MatchResult : IMatchResult
    {
        private IOnigCaptureIndex[] captureIndexes;
        private int? matchedRuleId;

        internal MatchResult(IOnigCaptureIndex[] captureIndexes, int? matchedRuleId)
        {
            this.captureIndexes = captureIndexes;
            this.matchedRuleId = matchedRuleId;
        }

        public IOnigCaptureIndex[] GetCaptureIndices()
        {
            return captureIndexes;
        }

        public int? GetMatchedRuleId()
        {
            return matchedRuleId;
        }
    }
}
