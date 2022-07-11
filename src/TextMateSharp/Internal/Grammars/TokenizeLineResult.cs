using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars
{
    public class TokenizeLineResult : ITokenizeLineResult
    {
        public IToken[] Tokens { get; private set; }
        public IStackElement RuleStack { get; private set; }

        public TokenizeLineResult(IToken[] tokens, IStackElement ruleStack)
        {
            Tokens = tokens;
            RuleStack = ruleStack;
        }
    }
}