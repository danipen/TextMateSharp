using TextMateSharp.Internal.Oniguruma;
using TextMateSharp.Internal.Rules;

namespace TextMateSharp.Internal.Matcher
{
    public interface IMatchResult
    {
        IOnigCaptureIndex[] CaptureIndexes { get; }

        RuleId MatchedRuleId { get; }
    }
}
