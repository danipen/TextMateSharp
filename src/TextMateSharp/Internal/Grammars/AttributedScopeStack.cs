using System;
using System.Collections.Generic;

using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars
{
    public class AttributedScopeStack
    {
        public AttributedScopeStack Parent { get; private set; }
        public string ScopePath { get; private set; }
        public int TokenAttributes { get; private set; }
        private List<string> _cachedScopeNames;

        public AttributedScopeStack(AttributedScopeStack parent, string scopePath, int tokenAttributes)
        {
            Parent = parent;
            ScopePath = scopePath;
            TokenAttributes = tokenAttributes;
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

                if (a.ScopePath != b.ScopePath || a.TokenAttributes != b.TokenAttributes)
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
                   ScopePath.GetHashCode() +
                   TokenAttributes.GetHashCode();
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
                if (MatchesScope(target.ScopePath, selector, selectorWithDot))
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

        public static int MergeAttributes(
            int existingTokenAttributes,
            AttributedScopeStack scopesList,
            BasicScopeAttributes basicScopeAttributes)
        {
            if (basicScopeAttributes == null)
            {
                return existingTokenAttributes;
            }

            FontStyle fontStyle = FontStyle.NotSet;
            int foreground = 0;
            int background = 0;

            if (basicScopeAttributes.ThemeData != null)
            {
                // Find the first themeData that matches
                foreach (ThemeTrieElementRule themeData in basicScopeAttributes.ThemeData)
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

            return EncodedTokenAttributes.Set(
                existingTokenAttributes,
                basicScopeAttributes.LanguageId,
                basicScopeAttributes.TokenType,
                null,
                fontStyle,
                foreground,
                background);
        }

        private static AttributedScopeStack Push(AttributedScopeStack target, Grammar grammar, List<string> scopes)
        {
            foreach (string scope in scopes)
            {
                target = PushSingleScope(target, grammar, scope);
            }
            return target;
        }

        private static AttributedScopeStack PushSingleScope(AttributedScopeStack target, Grammar grammar, string scope)
        {
            BasicScopeAttributes rawMetadata = grammar.GetMetadataForScope(scope);
            int metadata = AttributedScopeStack.MergeAttributes(target.TokenAttributes, target, rawMetadata);
            return new AttributedScopeStack(target, scope, metadata);
        }

        public AttributedScopeStack PushAtributed(string scopePath, Grammar grammar)
        {
            if (scopePath == null)
            {
                return this;
            }
            if (scopePath.IndexOf(' ') >= 0)
            {
                // there are multiple scopes to push
                return Push(this, grammar, new List<string>(scopePath.Split(new[] {" "}, StringSplitOptions.None)));
            }
            // there is a single scope to push - avoid List allocation
            return PushSingleScope(this, grammar, scopePath);
        }

        public List<string> GetScopeNames()
        {
            if (_cachedScopeNames == null)
            {
                _cachedScopeNames = GenerateScopes(this);
            }
            return _cachedScopeNames;
        }

        private static List<string> GenerateScopes(AttributedScopeStack scopesList)
        {
            List<string> result = new List<string>();
            while (scopesList != null)
            {
                result.Add(scopesList.ScopePath);
                scopesList = scopesList.Parent;
            }
            result.Reverse();
            return result;
        }
    }
}