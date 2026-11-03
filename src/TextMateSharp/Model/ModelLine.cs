using System.Collections.Generic;

namespace TextMateSharp.Model
{
    public class ModelLine
    {
        public bool IsInvalid { get; set; }
        public TMState State { get; set; }
        public List<TMToken> Tokens { get; set; }

        // Bumped on every invalidation, under TMModel's lock, so a tokenizer run can tell
        // that the line was invalidated after the run started on it
        internal int Version { get; set; }

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