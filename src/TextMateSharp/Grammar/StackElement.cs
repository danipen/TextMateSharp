using System;
using System.Collections.Generic;

using TextMateSharp.Internal.Grammars;
using TextMateSharp.Internal.Rules;

namespace TextMateSharp.Grammars
{
    public interface IStackElement
    {
        int Depth { get; }
    }

    public class StackElement : IStackElement
    {
        public static StackElement NULL = new StackElement(null, 0, 0, null, null, null);

        public StackElement Parent { get; private set; }
        public int Depth { get; private set; }
        public int? RuleId { get; private set; }
        public string EndRule { get; private set; }
        public ScopeListElement NameScopesList { get; private set; }
        public ScopeListElement ContentNameScopesList { get; private set; }

        private int _enterPosition;

        public StackElement(
            StackElement parent,
            int? ruleId,
            int enterPos,
            string endRule,
            ScopeListElement nameScopesList,
            ScopeListElement contentNameScopesList)
        {
            Parent = parent;
            Depth = (this.Parent != null ? this.Parent.Depth + 1 : 1);
            RuleId = ruleId;
            EndRule = endRule;
            NameScopesList = nameScopesList;
            ContentNameScopesList = contentNameScopesList;

            _enterPosition = enterPos;
        }

        private static bool StructuralEquals(StackElement a, StackElement b)
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
            if (!(other is StackElement)) {
                return false;
            }
            StackElement stackElement = (StackElement)other;
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
            StackElement el = this;
            while (el != null)
            {
                el._enterPosition = -1;
                el = el.Parent;
            }
        }

        public StackElement Pop()
        {
            return this.Parent;
        }

        public StackElement SafePop()
        {
            if (this.Parent != null)
            {
                return this.Parent;
            }
            return this;
        }

        public StackElement Push(int? ruleId, int enterPos, string endRule, ScopeListElement nameScopesList, ScopeListElement contentNameScopesList)
        {
            return new StackElement(this, ruleId, enterPos, endRule, nameScopesList, contentNameScopesList);
        }

        public int GetEnterPos()
        {
            return this._enterPosition;
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

        public StackElement setContentNameScopesList(ScopeListElement contentNameScopesList)
        {
            if (this.ContentNameScopesList.Equals(contentNameScopesList))
            {
                return this;
            }
            return this.Parent.Push(this.RuleId, this._enterPosition, this.EndRule, this.NameScopesList, contentNameScopesList);
        }

        public StackElement SetEndRule(string endRule)
        {
            if (this.EndRule != null && this.EndRule.Equals(endRule))
            {
                return this;
            }
            return new StackElement(this.Parent, this.RuleId, this._enterPosition, endRule, this.NameScopesList, this.ContentNameScopesList);
        }

        public bool HasSameRuleAs(StackElement other)
        {
            return this.RuleId == other.RuleId;
        }
    }
}