using System.Collections.Generic;

namespace TextMateSharp.Model
{
    public class LineTokens
    {
        public List<TMToken> Tokens;
        public int ActualStopOffset;
        public TMState EndState;

        public LineTokens(List<TMToken> tokens, int actualStopOffset, TMState endState)
        {
            this.Tokens = tokens;
            this.ActualStopOffset = actualStopOffset;
            this.EndState = endState;
        }

        public TMState GetEndState()
        {
            return EndState;
        }

        public void SetEndState(TMState endState)
        {
            this.EndState = endState;
        }

        public List<TMToken> GetTokens()
        {
            return Tokens;
        }
    }
}