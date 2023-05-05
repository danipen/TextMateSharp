using System;
using System.Collections.Generic;

namespace TextMateSharp.Themes
{
    public class ThemeTrieElementRule
    {
        // _themeTrieElementRuleBrand: void;

        public int scopeDepth;
        public List<string> parentScopes;
        public int fontStyle;
        public int foreground;
        public int background;
        public string name;

        public ThemeTrieElementRule(string name, int scopeDepth, List<string> parentScopes, int fontStyle, int foreground,
                int background)
        {
            this.name = name;
            this.scopeDepth = scopeDepth;
            this.parentScopes = parentScopes;
            this.fontStyle = fontStyle;
            this.foreground = foreground;
            this.background = background;
        }

        public ThemeTrieElementRule Clone()
        {
            return new ThemeTrieElementRule(this.name, this.scopeDepth, this.parentScopes, this.fontStyle, this.foreground,
                    this.background);
        }

        public static List<ThemeTrieElementRule> cloneArr(List<ThemeTrieElementRule> arr)
        {
            List<ThemeTrieElementRule> r = new List<ThemeTrieElementRule>();
            for (int i = 0, len = arr.Count; i < len; i++)
            {
                r.Add(arr[i].Clone());
            }
            return r;
        }

        public void AcceptOverwrite(string name, int scopeDepth, int fontStyle, int foreground, int background)
        {
            if (this.scopeDepth > scopeDepth)
            {
                // console.log('how did this happen?');
            }
            else
            {
                this.scopeDepth = scopeDepth;
            }
            // console.log('TODO -> my depth: ' + this.scopeDepth + ', overwriting depth: ' + scopeDepth);
            if (fontStyle != FontStyle.NotSet)
            {
                this.fontStyle = fontStyle;
            }
            if (foreground != 0)
            {
                this.foreground = foreground;
            }
            if (background != 0)
            {
                this.background = background;
            }
            if (!string.IsNullOrEmpty(name))
            {
                this.name = name;
            }
        }

        public override int GetHashCode()
        {
            return background.GetHashCode() +
                fontStyle.GetHashCode() +
                foreground.GetHashCode() +
                parentScopes.GetHashCode() +
                scopeDepth.GetHashCode();
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
            ThemeTrieElementRule other = (ThemeTrieElementRule)obj;
            return background == other.background && fontStyle == other.fontStyle && foreground == other.foreground &&
                Object.Equals(parentScopes, other.parentScopes) && scopeDepth == other.scopeDepth;
        }
    }
}