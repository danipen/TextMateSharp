using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars
{
    internal readonly struct TokenizeStringResult
    {
        internal StateStack Stack { get; }
        internal bool StoppedEarly { get; }

        internal TokenizeStringResult(StateStack stack, bool stoppedEarly)
        {
            this.Stack = stack;
            this.StoppedEarly = stoppedEarly;
        }
    }
}
