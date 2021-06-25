namespace TextMateSharp.Internal.Matcher
{
    public interface IMatchInjectionsResult : IMatchResult
    {
        bool IsPriorityMatch();
    }
}
