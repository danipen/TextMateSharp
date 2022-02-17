using System.Collections.Generic;

namespace TextMateSharp.Model
{
    public class ModelLine
    {
        public bool IsInvalid { get; set; }
        public TMState State { get; set; }
        public List<TMToken> Tokens { get; set; }

        public ModelLine()
        {
            IsInvalid = true;
        }

        public void ResetTokenizationState()
        {
            State = null;
            Tokens = null;
        }
    }
}