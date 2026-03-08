using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars
{
    internal sealed class TokenizeLineResult : ITokenizeLineResult
    {
        public IToken[] Tokens { get; private set; }
        public IStateStack RuleStack { get; private set; }
        public bool StoppedEarly { get; private set; }
        internal TokenizeLineResult(IToken[] tokens, IStateStack ruleStack, bool stoppedEarly)
        {
            Tokens = tokens;
            RuleStack = ruleStack;
            StoppedEarly = stoppedEarly;
        }
    }
}