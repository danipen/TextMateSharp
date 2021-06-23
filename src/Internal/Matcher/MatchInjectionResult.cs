using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Internal.Matcher
{
    internal class MatchInjectionsResult : IMatchInjectionsResult
    {
        private IOnigCaptureIndex[] captureIndexes;
        private int? matchedRuleId;
        private bool isPriorityMatch;

        internal MatchInjectionsResult(
            IOnigCaptureIndex[] captureIndexes,
            int? matchedRuleId,
            bool isPriorityMatch)
        {
            this.captureIndexes = captureIndexes;
            this.matchedRuleId = matchedRuleId;
            this.isPriorityMatch = isPriorityMatch;
        }

        public IOnigCaptureIndex[] GetCaptureIndices()
        {
            return captureIndexes;
        }

        public int? GetMatchedRuleId()
        {
            return matchedRuleId;
        }

        public bool IsPriorityMatch()
        {
            return isPriorityMatch;
        }
    }
}
