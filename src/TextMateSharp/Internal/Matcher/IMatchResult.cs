using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Internal.Matcher
{
    public interface IMatchResult
    {
        IOnigCaptureIndex[] CaptureIndexes { get; }

        int? MatchedRuleId { get; }
    }
}
