namespace TextMateSharp.Grammars
{
    public interface ITokenizeLineResult
    {
        IToken[] GetTokens();
        StackElement GetRuleStack();
    }
}
