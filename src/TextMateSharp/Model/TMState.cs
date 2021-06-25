using TextMateSharp.Grammars;

namespace TextMateSharp.Model
{
    public class TMState
    {
        private TMState parentEmbedderState;
        private StackElement ruleStack;

        public TMState(TMState parentEmbedderState, StackElement ruleStatck)
        {
            this.parentEmbedderState = parentEmbedderState;
            this.ruleStack = ruleStatck;
        }

        public void setRuleStack(StackElement ruleStack)
        {
            this.ruleStack = ruleStack;
        }

        public StackElement GetRuleStack()
        {
            return ruleStack;
        }

        public TMState Clone()
        {
            TMState parentEmbedderStateClone = this.parentEmbedderState != null ? 
                this.parentEmbedderState.Clone() : null;

            return new TMState(parentEmbedderStateClone, this.ruleStack);
        }

        public override bool Equals(object other)
        {
            if (!(other is TMState))
            {
                return false;
            }

            TMState otherState = (TMState)other;

            return Equals(parentEmbedderState, otherState.parentEmbedderState) &&
                   Equals(ruleStack, otherState.ruleStack);
        }

        public override int GetHashCode()
        {
            return this.parentEmbedderState.GetHashCode() + this.ruleStack.GetHashCode();
        }

    }
}