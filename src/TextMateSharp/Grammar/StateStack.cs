using System;
using System.Collections.Generic;

using TextMateSharp.Internal.Grammars;
using TextMateSharp.Internal.Rules;

namespace TextMateSharp.Grammars
{
    public interface IStateStack
    {
        int Depth { get; }
    }

    public class StateStack : IStateStack
    {
        public static StateStack NULL = new StateStack(
            null,
            RuleId.NO_RULE,
            0,
            0,
            false,
            null,
            null,
            null);

        public StateStack Parent { get; private set; }
        public int Depth { get; private set; }
        public RuleId RuleId { get; private set; }
        public string EndRule { get; private set; }
        public AttributedScopeStack NameScopesList { get; private set; }
        public AttributedScopeStack ContentNameScopesList { get; private set; }
        public bool BeginRuleCapturedEOL { get; private set; }

        private int _enterPos;
        private int _anchorPos;

        public StateStack(
            StateStack parent,
            RuleId ruleId,
            int enterPos,
            int anchorPos,
            bool beginRuleCapturedEOL,
            string endRule,
            AttributedScopeStack nameScopesList,
            AttributedScopeStack contentNameScopesList)
        {
            Parent = parent;
            Depth = (this.Parent != null ? this.Parent.Depth + 1 : 1);
            RuleId = ruleId;
            BeginRuleCapturedEOL = beginRuleCapturedEOL;
            EndRule = endRule;
            NameScopesList = nameScopesList;
            ContentNameScopesList = contentNameScopesList;

            _enterPos = enterPos;
            _anchorPos = anchorPos;
        }

        private static bool StructuralEquals(StateStack a, StateStack b)
        {
            if (a == b)
            {
                return true;
            }
            if (a == null || b == null)
            {
                return false;
            }
            return a.Depth == b.Depth && a.RuleId == b.RuleId && Equals(a.EndRule, b.EndRule) && StructuralEquals(a.Parent, b.Parent);
        }

        public override bool Equals(Object other)
        {
            if (other == this)
            {
                return true;
            }
            if (other == null)
            {
                return false;
            }
            if (!(other is StateStack)) {
                return false;
            }
            StateStack stackElement = (StateStack)other;
            return StructuralEquals(this, stackElement) && this.ContentNameScopesList.Equals(stackElement.ContentNameScopesList);
        }

        public override int GetHashCode()
        {
            return Depth.GetHashCode() + 
                RuleId.GetHashCode() +
                EndRule.GetHashCode() + 
                Parent.GetHashCode() +
                ContentNameScopesList.GetHashCode();
        }

        public void Reset()
        {
            StateStack el = this;
            while (el != null)
            {
                el._enterPos = -1;
                el = el.Parent;
            }
        }

        public StateStack Pop()
        {
            return this.Parent;
        }

        public StateStack SafePop()
        {
            if (this.Parent != null)
            {
                return this.Parent;
            }
            return this;
        }

        public StateStack Push(
            RuleId ruleId,
            int enterPos,
            int anchorPos,
            bool beginRuleCapturedEOL,
            string endRule,
            AttributedScopeStack nameScopesList,
            AttributedScopeStack contentNameScopesList)
        {
            return new StateStack(
                this,
                ruleId,
                enterPos,
                anchorPos,
                beginRuleCapturedEOL,
                endRule,
                nameScopesList,
                contentNameScopesList);
        }

        public int GetEnterPos()
        {
            return this._enterPos;
        }

        public int GetAnchorPos()
        {
            return this._anchorPos;
        }

        public Rule GetRule(IRuleRegistry grammar)
        {
            return grammar.GetRule(this.RuleId);
        }

        private void AppendString(List<string> res)
        {
            if (this.Parent != null)
            {
                this.Parent.AppendString(res);
            }

            res.Add('(' + this.RuleId.ToString() + ')'); //, TODO-${this.nameScopesList}, TODO-${this.contentNameScopesList})`;
        }

        public override string ToString()
        {
            List<string> r = new List<string>();
            this.AppendString(r);
            return '[' + string.Join(", ", r) + ']';
        }

        public StateStack WithContentNameScopesList(AttributedScopeStack contentNameScopesList)
        {
            if (this.ContentNameScopesList.Equals(contentNameScopesList))
            {
                return this;
            }
            return this.Parent.Push(
                this.RuleId,
                this._enterPos,
                this._anchorPos,
                this.BeginRuleCapturedEOL,
                this.EndRule,
                this.NameScopesList,
                contentNameScopesList);
        }

        public StateStack WithEndRule(string endRule)
        {
            if (this.EndRule != null && this.EndRule.Equals(endRule))
            {
                return this;
            }
            return new StateStack(
                this.Parent,
                this.RuleId,
                this._enterPos,
                this._anchorPos,
                this.BeginRuleCapturedEOL,
                endRule,
                this.NameScopesList,
                this.ContentNameScopesList);
        }

        public bool HasSameRuleAs(StateStack other)
        {
            return this.RuleId == other.RuleId;
        }
    }
}