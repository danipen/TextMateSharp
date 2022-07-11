using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars
{
    public class TokenizeLineResult2 : ITokenizeLineResult2
    {
        public int[] Tokens { get; private set; }
        public IStackElement RuleStack { get; private set; }

        public TokenizeLineResult2(int[] tokens, IStackElement ruleStack)
        {
            Tokens = tokens;
            RuleStack = ruleStack;
        }
    }
}