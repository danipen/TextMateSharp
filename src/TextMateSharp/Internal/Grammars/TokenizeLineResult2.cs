using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars
{
    public class TokenizeLineResult2 : ITokenizeLineResult2
    {
        public int[] Tokens { get; private set; }
        public IStateStack RuleStack { get; private set; }

        public TokenizeLineResult2(int[] tokens, IStateStack ruleStack)
        {
            Tokens = tokens;
            RuleStack = ruleStack;
        }
    }
}