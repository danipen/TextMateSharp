using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Internal.Matcher
{
    internal class MatchInjectionsResult : IMatchInjectionsResult
    {
        public IOnigCaptureIndex[] CaptureIndexes { get; private set; }
        public int? MatchedRuleId { get; private set; }
        public bool IsPriorityMatch { get; private set; }

        internal MatchInjectionsResult(
            IOnigCaptureIndex[] captureIndexes,
            int? matchedRuleId,
            bool isPriorityMatch)
        {
            CaptureIndexes = captureIndexes;
            MatchedRuleId = matchedRuleId;
            IsPriorityMatch = isPriorityMatch;
        }
    }
}
