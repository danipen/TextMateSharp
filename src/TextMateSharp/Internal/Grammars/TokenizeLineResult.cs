using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars
{
    public class TokenizeLineResult : ITokenizeLineResult
    {
        public IToken[] Tokens { get; private set; }
        public IStateStack RuleStack { get; private set; }

        public TokenizeLineResult(IToken[] tokens, IStateStack ruleStack)
        {
            Tokens = tokens;
            RuleStack = ruleStack;
        }
    }
}