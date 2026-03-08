using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars
{
    internal sealed class TokenizeLineResult2 : ITokenizeLineResult2
    {
        public int[] Tokens { get; private set; }
        public IStateStack RuleStack { get; private set; }
        internal bool StoppedEarly { get; private set; }

        public TokenizeLineResult2(int[] tokens, IStateStack ruleStack, bool stoppedEarly)
        {
            Tokens = tokens;
            RuleStack = ruleStack;
            StoppedEarly = stoppedEarly;
        }
    }
}