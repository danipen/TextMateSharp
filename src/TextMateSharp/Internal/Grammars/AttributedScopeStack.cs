using System;
using System.Collections.Generic;

using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars
{
    public class AttributedScopeStack : IEquatable<AttributedScopeStack>
    {
        public AttributedScopeStack Parent { get; private set; }
        public string ScopePath { get; private set; }
        public int TokenAttributes { get; private set; }
        private List<string> _cachedScopeNames;

        // Precomputed, per-node hash code (persistent structure => safe as long as instances are immutable)
        private readonly int _hashCode;

        public AttributedScopeStack(AttributedScopeStack parent, string scopePath, int tokenAttributes)
        {
            Parent = parent;
            ScopePath = scopePath;
            TokenAttributes = tokenAttributes;
            _hashCode = ComputeHashCode(parent, scopePath, tokenAttributes);
        }

        private static bool StructuralEquals(AttributedScopeStack a, AttributedScopeStack b)
        {
            while (true)
            {
                // Use ReferenceEquals to avoid infinite recursion through operator ==
                if (ReferenceEquals(a, b))
                {
                    return true;
                }

                if (a is null || b is null)
                {
                    // End of list reached only for one
                    return false;
                }

                if (!string.Equals(a.ScopePath, b.ScopePath, StringComparison.Ordinal) ||
                    a.TokenAttributes != b.TokenAttributes)
                {
                    return false;
                }

                // Go to previous pair
                a = a.Parent;
                b = b.Parent;
            }
        }

        // Internal so StateStack can perform null-safe equality checks on
        // ContentNameScopesList / NameScopesList without going through the
        // instance Equals (which would throw on null receivers)
        internal static bool Equals(AttributedScopeStack a, AttributedScopeStack b)
        {
            // Use ReferenceEquals to avoid infinite recursion through operator ==
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a is null || b is null)
            {
                return false;
            }

            // Precomputed hash codes let us reject non-equal pairs in O(1)
            // before walking the O(n) parent chain in StructuralEquals
            if (a._hashCode != b._hashCode)
            {
                return false;
            }

            return StructuralEquals(a, b);
        }

        /// <summary>
        /// Determines whether the specified <see cref="AttributedScopeStack"/> instance is equal to the current
        /// instance.
        /// </summary>
        /// <param name="other">The <see cref="AttributedScopeStack"/> instance to compare with the current instance.</param>
        /// <returns>true if the specified <see cref="AttributedScopeStack"/> is equal to the current instance; otherwise, false.</returns>
        public bool Equals(AttributedScopeStack other)
        {
            return Equals(this, other);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current instance.
        /// </summary>
        /// <remarks>This method overrides the base Object.Equals implementation to provide value equality
        /// specific to AttributedScopeStack instances.</remarks>
        /// <param name="other">The object to compare with the current instance.</param>
        /// <returns>true if the specified object is equal to the current instance; otherwise, false.</returns>
        public override bool Equals(object other)
        {
            if (other is AttributedScopeStack attributedScopeStack)
                return Equals(this, attributedScopeStack);

            return false;
        }

        /// <summary>
        /// Returns a hash code for the current instance, suitable for use in hashing algorithms and data structures
        /// such as hash tables.
        /// </summary>
        /// <remarks>Equal instances are guaranteed to return the same hash code. This method is typically
        /// used to support efficient lookups in hash-based collections.</remarks>
        /// <returns>An integer that represents the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            return _hashCode;
        }

        /// <summary>
        /// Determines whether two instances of <see cref="AttributedScopeStack"/> are equal.
        /// </summary>
        /// <remarks>This operator uses the <see cref="Equals(AttributedScopeStack, AttributedScopeStack)"/> method to determine
        /// equality.</remarks>
        /// <param name="left">The first <see cref="AttributedScopeStack"/> instance to compare.</param>
        /// <param name="right">The second <see cref="AttributedScopeStack"/> instance to compare.</param>
        /// <returns>true if the specified instances are equal; otherwise, false.</returns>
        public static bool operator ==(AttributedScopeStack left, AttributedScopeStack right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two instances of <see cref="AttributedScopeStack"/> are not equal.
        /// </summary>
        /// <remarks>This operator uses the <see cref="Equals(AttributedScopeStack, AttributedScopeStack)"/>
        /// method to evaluate equality.</remarks>
        /// <param name="left">The first <see cref="AttributedScopeStack"/> to compare.</param>
        /// <param name="right">The second <see cref="AttributedScopeStack"/> to compare.</param>
        /// <returns>true if the specified instances are not equal; otherwise, false.</returns>
        public static bool operator !=(AttributedScopeStack left, AttributedScopeStack right)
        {
            return !Equals(left, right);
        }

        private static int ComputeHashCode(AttributedScopeStack parent, string scopePath, int tokenAttributes)
        {
            const int primeFactor = 31; // Common prime factor for multiply-accumulate hash code
            const int seed = 17; // Common seed for hash code computation (different from primeFactor to reduce collisions)
            unchecked
            {
                int hash = parent?._hashCode ?? seed;
                hash = (hash * primeFactor) + tokenAttributes;

                var scopeHashCode = scopePath == null ? 0 : StringComparer.Ordinal.GetHashCode(scopePath);
                return (hash * primeFactor) + scopeHashCode;
            }
        }

        static bool MatchesScope(string scope, string selector)
        {
            if (scope == null || selector == null)
            {
                return false;
            }

            int selectorLen = selector.Length;
            int scopeLen = scope.Length;

            if (scopeLen == selectorLen)
                return string.Equals(scope, selector, StringComparison.Ordinal);

            // scope must be longer than selector and have a '.' immediately after the selector prefix
            if (scopeLen > selectorLen && scope[selectorLen] == '.')
                return string.CompareOrdinal(scope, 0, selector, 0, selectorLen) == 0;

            return false;
        }

        static bool Matches(AttributedScopeStack target, List<string> parentScopes)
        {
            if (parentScopes == null || parentScopes.Count == 0)
            {
                return true;
            }

            int len = parentScopes.Count;
            int index = 0;
            string selector = parentScopes[index];

            while (target != null)
            {
                if (MatchesScope(target.ScopePath, selector))
                {
                    index++;
                    if (index == len)
                    {
                        return true;
                    }
                    selector = parentScopes[index];
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
                List<ThemeTrieElementRule> themeDataList = basicScopeAttributes.ThemeData;
                for (int i = 0; i < themeDataList.Count; i++)
                {
                    ThemeTrieElementRule themeData = themeDataList[i];
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

        private static AttributedScopeStack Push(AttributedScopeStack target, Grammar grammar, string scopePath)
        {
            ReadOnlySpan<char> remaining = scopePath.AsSpan();

            // Use while(true) instead of while(remaining.Length > 0) to match
            // StringSplitOptions.None behavior: if the string ends with a space, the final
            // slice produces an empty span, and we must still push that empty segment
            // (e.g. "a b " => push "a", "b", "")
            while (true)
            {
                int spaceIndex = remaining.IndexOf(' ');
                if (spaceIndex < 0)
                {
                    target = PushSingleScope(target, grammar, GetScopeSlice(scopePath, remaining));
                    break;
                }

                target = PushSingleScope(target, grammar, GetScopeSlice(scopePath, remaining.Slice(0, spaceIndex)));
                remaining = remaining.Slice(spaceIndex + 1);
            }
            return target;
        }

        private static string GetScopeSlice(string scopePath, ReadOnlySpan<char> slice)
        {
            if (slice.IsEmpty)
            {
                return string.Empty;
            }

            if (slice.Length == scopePath.Length)
            {
                return scopePath;
            }

            return slice.ToString();
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
            if (grammar == null) throw new ArgumentNullException(nameof(grammar));

            if (scopePath.IndexOf(' ') >= 0)
            {
                // there are multiple scopes to push
                return Push(this, grammar, scopePath);
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

        /// <summary>
        /// Returns a string representation of this scope stack, with scope names separated by spaces.
        /// </summary>
        /// <returns>A space-separated string of scope names from root to leaf.</returns>
        public override string ToString()
        {
            return string.Join(" ", GetScopeNames());
        }

        private static List<string> GenerateScopes(AttributedScopeStack scopesList)
        {
            // First pass: count depth to pre-size the list
            int depth = 0;
            AttributedScopeStack current = scopesList;
            while (current != null)
            {
                depth++;
                current = current.Parent;
            }

            // initialize exact capacity to avoid resizing
            List<string> result = new List<string>(depth);
            current = scopesList;
            while (current != null)
            {
                result.Add(current.ScopePath);
                current = current.Parent;
            }

            result.Reverse();
            return result;
        }
    }
}