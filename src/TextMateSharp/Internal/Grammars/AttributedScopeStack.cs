using System;
using System.Collections.Generic;

using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars
{
    public class AttributedScopeStack
    {
        public AttributedScopeStack Parent { get; private set; }
        public string Scope { get; private set; }
        public int Metadata { get; private set; }

        public AttributedScopeStack(AttributedScopeStack parent, string scope, int metadata)
        {
            Parent = parent;
            Scope = scope;
            Metadata = metadata;
        }

        private static bool StructuralEquals(AttributedScopeStack a, AttributedScopeStack b)
        {
            do
            {
                if (a == b)
                {
                    return true;
                }

                if (a == null && b == null)
                {
                    // End of list reached for both
                    return true;
                }

                if (a == null || b == null)
                {
                    // End of list reached only for one
                    return false;
                }

                if (a.Scope != b.Scope || a.Metadata != b.Metadata)
                {
                    return false;
                }

                // Go to previous pair
                a = a.Parent;
                b = b.Parent;
            } while (true);
        }

        private static bool Equals(AttributedScopeStack a, AttributedScopeStack b)
        {
            if (a == b)
            {
                return true;
            }
            if (a == null || b == null)
            {
                return false;
            }
            return StructuralEquals(a, b);
        }

        public override bool Equals(object other)
        {
            if (other == null || (other is AttributedScopeStack))
                return false;

            return Equals(this, (AttributedScopeStack)other);
        }

        public override int GetHashCode()
        {
            return Parent.GetHashCode() +
                   Scope.GetHashCode() +
                   Metadata.GetHashCode();
        }


        static bool MatchesScope(string scope, string selector, string selectorWithDot)
        {
            return (selector.Equals(scope) || scope.StartsWith(selectorWithDot));
        }

        static bool Matches(AttributedScopeStack target, List<string> parentScopes)
        {
            if (parentScopes == null)
            {
                return true;
            }

            int len = parentScopes.Count;
            int index = 0;
            string selector = parentScopes[index];
            string selectorWithDot = selector + ".";

            while (target != null)
            {
                if (MatchesScope(target.Scope, selector, selectorWithDot))
                {
                    index++;
                    if (index == len)
                    {
                        return true;
                    }
                    selector = parentScopes[index];
                    selectorWithDot = selector + '.';
                }
                target = target.Parent;
            }

            return false;
        }

        public static int MergeMetadata(int metadata, AttributedScopeStack scopesList, BasicScopeAttributes source)
        {
            if (source == null)
            {
                return metadata;
            }

            int fontStyle = FontStyle.NotSet;
            int foreground = 0;
            int background = 0;

            if (source.ThemeData != null)
            {
                // Find the first themeData that matches
                foreach (ThemeTrieElementRule themeData in source.ThemeData)
                {
                    if (Matches(scopesList, themeData.parentScopes))
                    {
                        fontStyle = themeData.fontStyle;
                        foreground = themeData.foreground;
                        background = themeData.background;
                        break;
                    }
                }
            }

            return EncodedTokenAttributes.Set(metadata, source.LanguageId, source.TokenType, null,
                fontStyle, foreground, background);
        }

        private static AttributedScopeStack Push(AttributedScopeStack target, Grammar grammar, List<string> scopes)
        {
            foreach (string scope in scopes)
            {
                BasicScopeAttributes rawMetadata = grammar.GetMetadataForScope(scope);
                int metadata = AttributedScopeStack.MergeMetadata(target.Metadata, target, rawMetadata);
                target = new AttributedScopeStack(target, scope, metadata);
            }
            return target;
        }

        public AttributedScopeStack Push(Grammar grammar, string scope)
        {
            if (scope == null)
            {
                return this;
            }
            if (scope.IndexOf(' ') >= 0)
            {
                // there are multiple scopes to push
                return Push(this, grammar, new List<string>(scope.Split(new[] {" "}, StringSplitOptions.None)));
            }
            // there is a single scope to push
            return Push(this, grammar, new List<string>() { scope });
        }

        private static List<string> GenerateScopes(AttributedScopeStack scopesList)
        {
            List<string> result = new List<string>();
            while (scopesList != null)
            {
                result.Add(scopesList.Scope);
                scopesList = scopesList.Parent;
            }
            result.Reverse();
            return result;
        }

        public List<string> GenerateScopes()
        {
            return AttributedScopeStack.GenerateScopes(this);
        }
    }
}