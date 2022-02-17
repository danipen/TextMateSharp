using System.Collections.Generic;

namespace TextMateSharp.Model
{
    public class LineTokens
    {
        public List<TMToken> Tokens { get; private set; }
        public int ActualStopOffset { get; set; }
        public TMState EndState { get; set; }

        public LineTokens(List<TMToken> tokens, int actualStopOffset, TMState endState)
        {
            Tokens = tokens;
            ActualStopOffset = actualStopOffset;
            EndState = endState;
        }
    }
}