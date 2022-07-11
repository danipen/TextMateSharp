namespace TextMateSharp.Grammars
{
    public interface ITokenizeLineResult
    {
        IToken[] Tokens { get; }
        IStackElement RuleStack { get; }
    }
}
