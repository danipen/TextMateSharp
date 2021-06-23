using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars
{
    public class TokenizeLineResult2 : ITokenizeLineResult2
    {

        private int[] tokens;
        private StackElement ruleStack;

        public TokenizeLineResult2(int[] tokens, StackElement ruleStack)
        {
            this.tokens = tokens;
            this.ruleStack = ruleStack;
        }

        public int[] GetTokens()
        {
            return tokens;
        }

        public StackElement GetRuleStack()
        {
            return ruleStack;
        }
    }
}