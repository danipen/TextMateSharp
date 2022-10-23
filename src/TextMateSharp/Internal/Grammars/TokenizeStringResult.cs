using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars
{
    public class TokenizeStringResult
    {
        public StateStack Stack { get; private set; }
        public bool StoppedEarly { get; private set; }

        public TokenizeStringResult(StateStack stack, bool stoppedEarly)
        {
            this.Stack = stack;
            this.StoppedEarly = stoppedEarly;
        }
    }
}
