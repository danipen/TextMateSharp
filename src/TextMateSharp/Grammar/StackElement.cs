using System;
using System.Collections.Generic;

using TextMateSharp.Internal.Grammars;
using TextMateSharp.Internal.Rules;

namespace TextMateSharp.Grammars
{
    public class StackElement
    {
        public static StackElement NULL = new StackElement(null, 0, 0, null, null, null);

        private int enterPosition;
        public StackElement parent;
        public int depth;
        public int? ruleId;
        public string endRule;
        public ScopeListElement nameScopesList;
        public ScopeListElement contentNameScopesList;

        public StackElement(
            StackElement parent,
            int? ruleId,
            int enterPos,
            string endRule,
            ScopeListElement nameScopesList,
            ScopeListElement contentNameScopesList)
        {
            this.parent = parent;
            this.depth = (this.parent != null ? this.parent.depth + 1 : 1);
            this.ruleId = ruleId;
            this.enterPosition = enterPos;
            this.endRule = endRule;
            this.nameScopesList = nameScopesList;
            this.contentNameScopesList = contentNameScopesList;
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
            return a.depth == b.depth && a.ruleId == b.ruleId && Equals(a.endRule, b.endRule) && StructuralEquals(a.parent, b.parent);
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
            return StructuralEquals(this, stackElement) && this.contentNameScopesList.Equals(stackElement.contentNameScopesList);
        }

        public override int GetHashCode()
        {
            return depth.GetHashCode() + 
                ruleId.GetHashCode() +
                endRule.GetHashCode() + 
                parent.GetHashCode() +
                contentNameScopesList.GetHashCode();
        }

        public void Reset()
        {
            StackElement el = this;
            while (el != null)
            {
                el.enterPosition = -1;
                el = el.parent;
            }
        }

        public StackElement Pop()
        {
            return this.parent;
        }

        public StackElement SafePop()
        {
            if (this.parent != null)
            {
                return this.parent;
            }
            return this;
        }

        public StackElement Push(int? ruleId, int enterPos, string endRule, ScopeListElement nameScopesList, ScopeListElement contentNameScopesList)
        {
            return new StackElement(this, ruleId, enterPos, endRule, nameScopesList, contentNameScopesList);
        }

        public int GetEnterPos()
        {
            return this.enterPosition;
        }

        public Rule GetRule(IRuleRegistry grammar)
        {
            return grammar.GetRule(this.ruleId);
        }

        private void AppendString(List<string> res)
        {
            if (this.parent != null)
            {
                this.parent.AppendString(res);
            }

            res.Add('(' + this.ruleId.ToString() + ')'); //, TODO-${this.nameScopesList}, TODO-${this.contentNameScopesList})`;
        }

        public override string ToString()
        {
            List<string> r = new List<string>();
            this.AppendString(r);
            return '[' + string.Join(", ", r) + ']';
        }

        public StackElement setContentNameScopesList(ScopeListElement contentNameScopesList)
        {
            if (this.contentNameScopesList.Equals(contentNameScopesList))
            {
                return this;
            }
            return this.parent.Push(this.ruleId, this.enterPosition, this.endRule, this.nameScopesList, contentNameScopesList);
        }

        public StackElement SetEndRule(string endRule)
        {
            if (this.endRule != null && this.endRule.Equals(endRule))
            {
                return this;
            }
            return new StackElement(this.parent, this.ruleId, this.enterPosition, endRule, this.nameScopesList, this.contentNameScopesList);
        }

        public bool HasSameRuleAs(StackElement other)
        {
            return this.ruleId == other.ruleId;
        }
    }
}