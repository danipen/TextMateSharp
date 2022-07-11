namespace TextMateSharp.Grammars
{
    public interface ITokenizeLineResult
    {
        IToken[] Tokens { get; }
        IStateStack RuleStack { get; }
    }
}
