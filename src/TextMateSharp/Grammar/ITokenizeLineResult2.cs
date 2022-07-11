namespace TextMateSharp.Grammars
{
    public interface ITokenizeLineResult2
    {
        int[] Tokens { get; }
        IStateStack RuleStack { get; }
    }
}