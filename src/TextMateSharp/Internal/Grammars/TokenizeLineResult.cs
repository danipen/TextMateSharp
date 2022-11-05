using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars
{
    public class TokenizeLineResult : ITokenizeLineResult
    {
        public IToken[] Tokens { get; private set; }
        public IStateStack RuleStack { get; private set; }
        public bool StoppedEarly { get; private set; }
        public TokenizeLineResult(IToken[] tokens, IStateStack ruleStack, bool stoppedEarly)
        {
            Tokens = tokens;
            RuleStack = ruleStack;
            StoppedEarly = stoppedEarly;
        }
    }
}