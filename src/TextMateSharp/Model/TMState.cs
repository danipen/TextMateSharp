using TextMateSharp.Grammars;

namespace TextMateSharp.Model
{
    public class TMState
    {
        private TMState _parentEmbedderState;
        private IStateStack _ruleStack;

        public TMState(TMState parentEmbedderState, IStateStack ruleStatck)
        {
            this._parentEmbedderState = parentEmbedderState;
            this._ruleStack = ruleStatck;
        }

        public void SetRuleStack(IStateStack ruleStack)
        {
            this._ruleStack = ruleStack;
        }

        public IStateStack GetRuleStack()
        {
            return _ruleStack;
        }

        public TMState Clone()
        {
            TMState parentEmbedderStateClone = this._parentEmbedderState != null ? 
                this._parentEmbedderState.Clone() : null;

            return new TMState(parentEmbedderStateClone, this._ruleStack);
        }

        public override bool Equals(object other)
        {
            if (!(other is TMState))
            {
                return false;
            }

            TMState otherState = (TMState)other;

            return Equals(_parentEmbedderState, otherState._parentEmbedderState) &&
                   Equals(_ruleStack, otherState._ruleStack);
        }

        public override int GetHashCode()
        {
            return this._parentEmbedderState.GetHashCode() + this._ruleStack.GetHashCode();
        }

    }
}