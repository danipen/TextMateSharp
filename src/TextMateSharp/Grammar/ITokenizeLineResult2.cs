namespace TextMateSharp.Grammars
{
    public interface ITokenizeLineResult2
    {
        int[] Tokens { get; }
        IStackElement RuleStack { get; }
    }
}