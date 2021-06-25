namespace TextMateSharp.Grammars
{
    public interface ITokenizeLineResult2
    {
        int[] GetTokens();
        StackElement GetRuleStack();
    }
}