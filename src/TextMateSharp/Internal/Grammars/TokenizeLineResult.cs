using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars
{
    public class TokenizeLineResult : ITokenizeLineResult
    {
        public IToken[] Tokens { get; private set; }
        public StackElement RuleStack { get; private set; }

        public TokenizeLineResult(IToken[] tokens, StackElement ruleStack)
        {
            this.Tokens = tokens;
            this.RuleStack = ruleStack;
        }
    }
}