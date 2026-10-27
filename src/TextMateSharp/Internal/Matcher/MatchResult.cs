using TextMateSharp.Internal.Oniguruma;

using TextMateSharp.Internal.Rules;

namespace TextMateSharp.Internal.Matcher
{
    // A rule's or an injection's match; the default value is no match.
    readonly struct MatchResult
    {
        internal OnigCaptureIndex[] CaptureIndexes { get; }
        internal RuleId MatchedRuleId { get; }
        internal bool IsPriorityMatch { get; }

        internal MatchResult(OnigCaptureIndex[] captureIndexes, RuleId matchedRuleId, bool isPriorityMatch = false)
        {
            CaptureIndexes = captureIndexes;
            MatchedRuleId = matchedRuleId;
            IsPriorityMatch = isPriorityMatch;
        }

        internal bool IsMatch => CaptureIndexes != null;
    }
}
