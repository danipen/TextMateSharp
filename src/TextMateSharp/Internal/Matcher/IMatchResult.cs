using TextMateSharp.Internal.Oniguruma;

namespace TextMateSharp.Internal.Matcher
{
    public interface IMatchResult
    {
        IOnigCaptureIndex[] GetCaptureIndices();

        int? GetMatchedRuleId();
    }
}
