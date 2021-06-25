using System.Collections.Generic;

namespace TextMateSharp.Model
{
    public class ModelLine
    {
        public bool IsInvalid;
        public TMState State;
        public List<TMToken> Tokens;

        public void ResetTokenizationState()
        {
            this.State = null;
            this.Tokens = null;
        }

        public TMState GetState()
        {
            return State;
        }

        public void SetState(TMState state)
        {
            this.State = state;
        }

        public void SetTokens(List<TMToken> tokens)
        {
            this.Tokens = tokens;
        }

        public List<TMToken> GetTokens()
        {
            return Tokens;
        }
    }
}