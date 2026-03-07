using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars
{
    internal class TokenizeStringResult
    {
        internal StateStack Stack { get; private set; }
        internal bool StoppedEarly { get; private set; }

        internal TokenizeStringResult(StateStack stack, bool stoppedEarly)
        {
            this.Stack = stack;
            this.StoppedEarly = stoppedEarly;
        }
    }
}
