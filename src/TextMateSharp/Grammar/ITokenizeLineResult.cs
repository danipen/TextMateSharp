namespace TextMateSharp.Grammars
{
    public interface ITokenizeLineResult
    {
        IToken[] Tokens { get; }
        StackElement RuleStack { get; }
    }
}
