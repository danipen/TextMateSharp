using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars
{
    public class TokenizeLineResult : ITokenizeLineResult
    {
        private IToken[] tokens;
        private StackElement ruleStack;

        public TokenizeLineResult(IToken[] tokens, StackElement ruleStack)
        {
            this.tokens = tokens;
            this.ruleStack = ruleStack;
        }

        public IToken[] GetTokens()
        {
            return tokens;
        }

        public StackElement GetRuleStack()
        {
            return ruleStack;
        }
    }
}