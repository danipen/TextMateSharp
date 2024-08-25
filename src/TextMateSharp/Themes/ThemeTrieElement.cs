
using System;
using System.Collections.Generic;

using TextMateSharp.Internal.Utils;

namespace TextMateSharp.Themes
{
    public class ThemeTrieElement
    {

        // _themeTrieElementBrand: void;

        private ThemeTrieElementRule mainRule;
        private List<ThemeTrieElementRule> rulesWithParentScopes;
        private Dictionary<string /* segment */, ThemeTrieElement> children;

        public ThemeTrieElement(ThemeTrieElementRule mainRule) :
            this(mainRule, new List<ThemeTrieElementRule>(), new Dictionary<string /* segment */, ThemeTrieElement>())
        {
        }

        public ThemeTrieElement(ThemeTrieElementRule mainRule, List<ThemeTrieElementRule> rulesWithParentScopes) :
                this(mainRule, rulesWithParentScopes, new Dictionary<string /* segment */, ThemeTrieElement>())
        {

        }

        public ThemeTrieElement(ThemeTrieElementRule mainRule, List<ThemeTrieElementRule> rulesWithParentScopes,
                Dictionary<string /* segment */, ThemeTrieElement> children)
        {
            this.mainRule = mainRule;
            this.rulesWithParentScopes = rulesWithParentScopes;
            this.children = children;
        }

        private static List<ThemeTrieElementRule> SortBySpecificity(List<ThemeTrieElementRule> arr)
        {
            if (arr.Count == 1)
            {
                return arr;
            }
            arr.Sort((a, b) => CmpBySpecificity(a, b));
            return arr;
        }

        private static int CmpBySpecificity(ThemeTrieElementRule a, ThemeTrieElementRule b)
        {
            if (a.scopeDepth == b.scopeDepth)
            {
                List<string> aParentScopes = a.parentScopes;
                List<string> bParentScopes = b.parentScopes;
                int aParentScopesLen = aParentScopes == null ? 0 : aParentScopes.Count;
                int bParentScopesLen = bParentScopes == null ? 0 : bParentScopes.Count;
                if (aParentScopesLen == bParentScopesLen)
                {
                    for (int i = 0; i < aParentScopesLen; i++)
                    {
                        int aLen = aParentScopes[i].Length;
                        int bLen = bParentScopes[i].Length;
                        if (aLen != bLen)
                        {
                            return bLen - aLen;
                        }
                    }
                }
                return bParentScopesLen - aParentScopesLen;
            }
            return b.scopeDepth - a.scopeDepth;
        }

        public List<ThemeTrieElementRule> Match(string scope)
        {
            List<ThemeTrieElementRule> arr;
            if ("".Equals(scope))
            {
                arr = new List<ThemeTrieElementRule>();
                arr.Add(this.mainRule);
                arr.AddRange(this.rulesWithParentScopes);
                return ThemeTrieElement.SortBySpecificity(arr);
            }

            int dotIndex = scope.IndexOf('.');
            string head;
            string tail;
            if (dotIndex == -1)
            {
                head = scope;
                tail = "";
            }
            else
            {
                head = scope.SubstringAtIndexes(0, dotIndex);
                tail = scope.Substring(dotIndex + 1);
            }

            if (children.TryGetValue(head, out ThemeTrieElement value))
            {
                return value.Match(tail);
            }

            arr = new List<ThemeTrieElementRule>();
            if (this.mainRule.foreground > 0)
                arr.Add(this.mainRule);
            arr.AddRange(this.rulesWithParentScopes);
            return ThemeTrieElement.SortBySpecificity(arr);
        }

        public void Insert(string name, int scopeDepth, string scope, List<string> parentScopes, FontStyle fontStyle, int foreground,
                int background)
        {
            if ("".Equals(scope))
            {
                this.DoInsertHere(name, scopeDepth, parentScopes, fontStyle, foreground, background);
                return;
            }

            int dotIndex = scope.IndexOf('.');
            string head;
            string tail;
            if (dotIndex == -1)
            {
                head = scope;
                tail = "";
            }
            else
            {
                head = scope.SubstringAtIndexes(0, dotIndex);
                tail = scope.Substring(dotIndex + 1);
            }

            ThemeTrieElement child;
            if (children.TryGetValue(head, out ThemeTrieElement value))
            {
                child = value;
            }
            else
            {
                child = new ThemeTrieElement(this.mainRule.Clone(),
                        ThemeTrieElementRule.cloneArr(this.rulesWithParentScopes));
                this.children[head] = child;
            }

            child.Insert(name, scopeDepth + 1, tail, parentScopes, fontStyle, foreground, background);
        }

        private void DoInsertHere(string name, int scopeDepth, List<string> parentScopes, FontStyle fontStyle, int foreground,
                int background)
        {

            if (parentScopes == null)
            {
                // Merge into the main rule
                this.mainRule.AcceptOverwrite(name, scopeDepth, fontStyle, foreground, background);
                return;
            }

            // Try to merge into existing rule
            foreach (ThemeTrieElementRule rule in this.rulesWithParentScopes)
            {
                if (StringUtils.StrArrCmp(rule.parentScopes, parentScopes) == 0)
                {
                    // bingo! => we get to merge this into an existing one
                    rule.AcceptOverwrite(rule.name,  scopeDepth, fontStyle, foreground, background);
                    return;
                }
            }

            // Must add a new rule

            // Inherit from main rule
            if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(mainRule.name))
            {
                name = mainRule.name;
            }
            if (fontStyle == FontStyle.NotSet)
            {
                fontStyle = this.mainRule.fontStyle;
            }
            if (foreground == 0)
            {
                foreground = this.mainRule.foreground;
            }
            if (background == 0)
            {
                background = this.mainRule.background;
            }

            this.rulesWithParentScopes.Add(
                new ThemeTrieElementRule(name, scopeDepth, parentScopes, fontStyle, foreground, background));
        }

        public override int GetHashCode()
        {
            return children.GetHashCode() +
                      mainRule.GetHashCode() +
                      rulesWithParentScopes.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj == null)
            {
                return false;
            }
            if (GetType() != obj.GetType())
            {
                return false;
            }
            ThemeTrieElement other = (ThemeTrieElement)obj;
            return Object.Equals(children, other.children) && Object.Equals(mainRule, other.mainRule) && Object.Equals(rulesWithParentScopes, other.rulesWithParentScopes);
        }
    }
}
