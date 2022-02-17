using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars
{
    public class TokenizeLineResult2 : ITokenizeLineResult2
    {
        public int[] Tokens { get; private set; }
        public StackElement RuleStack { get; private set; }

        public TokenizeLineResult2(int[] tokens, StackElement ruleStack)
        {
            Tokens = tokens;
            RuleStack = ruleStack;
        }
    }
}