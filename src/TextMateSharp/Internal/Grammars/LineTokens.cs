using System.Collections.Generic;
using TextMateSharp.Grammars;
using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars
{
    internal sealed class LineTokens
    {
        // used only if `_emitBinaryTokens` is false.
        private List<IToken> _tokens;

        private readonly bool _emitBinaryTokens;

        // used only if `_emitBinaryTokens` is true.
        private List<int> binaryTokens;

        private int _lastTokenEndIndex = 0;
        private readonly List<TokenTypeMatcher> _tokenTypeOverrides;

        private readonly BalancedBracketSelectors _balancedBracketSelectors;

        internal LineTokens(
            bool emitBinaryTokens,
            List<TokenTypeMatcher> tokenTypeOverrides,
            BalancedBracketSelectors balancedBracketSelectors)
        {
            this._emitBinaryTokens = emitBinaryTokens;
            if (this._emitBinaryTokens)
            {
                this._tokens = null;
                this.binaryTokens = new List<int>();
            }
            else
            {
                this._tokens = new List<IToken>();
                this.binaryTokens = null;
            }
            this._tokenTypeOverrides = tokenTypeOverrides;
            this._balancedBracketSelectors = balancedBracketSelectors;
        }

        // A grammar reuses its line tokens for every line: keeps the lists unless a long line grew them.
        internal void Reset()
        {
            const int MaxRetainedCapacity = 1024;
            this._lastTokenEndIndex = 0;
            if (this._emitBinaryTokens)
            {
                if (this.binaryTokens.Capacity > 2 * MaxRetainedCapacity)
                {
                    this.binaryTokens = new List<int>();
                }
                this.binaryTokens.Clear();
            }
            else
            {
                if (this._tokens.Capacity > MaxRetainedCapacity)
                {
                    this._tokens = new List<IToken>();
                }
                this._tokens.Clear();
            }
        }

        internal void Produce(StateStack stack, int endIndex)
        {
            this.ProduceFromScopes(stack.ContentNameScopesList, endIndex);
        }

        internal void ProduceFromScopes(AttributedScopeStack scopesList, int endIndex)
        {
            if (this._lastTokenEndIndex >= endIndex)
            {
                return;
            }

            if (this._emitBinaryTokens)
            {
                int metadata = scopesList.TokenAttributes;

                var containsBalancedBrackets = false;
                var balancedBracketSelectors = _balancedBracketSelectors;
                if (balancedBracketSelectors != null && balancedBracketSelectors.MatchesAlways())
                {
                    containsBalancedBrackets = true;
                }

                if (_tokenTypeOverrides.Count > 0 || (balancedBracketSelectors != null
                        && !balancedBracketSelectors.MatchesAlways() && !balancedBracketSelectors.MatchesNever()))
                {
                    // Only generate scope array when required to improve performance
                    var scopes2 = scopesList.GetScopeNames();
                    foreach (var tokenType in _tokenTypeOverrides)
                    {
                        if (tokenType.Matcher.Invoke(scopes2))
                        {
                            metadata = EncodedTokenAttributes.Set(
                                    metadata,
                                    0,
                                    tokenType.Type, // toOptionalTokenType(tokenType.type),
                                    null,
                                    FontStyle.NotSet,
                                    0,
                                    0);
                        }
                    }
                    if (balancedBracketSelectors != null)
                    {
                        containsBalancedBrackets = balancedBracketSelectors.Match(scopes2);
                    }
                }

                if (containsBalancedBrackets)
                {
                    metadata = EncodedTokenAttributes.Set(
                            metadata,
                            0,
                            OptionalStandardTokenType.NotSet,
                            containsBalancedBrackets,
                            FontStyle.NotSet,
                            0,
                            0);
                }

                if (this.binaryTokens.Count != 0 && this.binaryTokens[this.binaryTokens.Count - 1] == metadata)
                {
                    // no need to push a token with the same metadata
                    this._lastTokenEndIndex = endIndex;
                    return;
                }

                this.binaryTokens.Add(this._lastTokenEndIndex);
                this.binaryTokens.Add(metadata);

                this._lastTokenEndIndex = endIndex;
                return;
            }

            List<string> scopes = scopesList.GetScopeNames();

            this._tokens.Add(new Token(
                this._lastTokenEndIndex >= 0 ? this._lastTokenEndIndex : 0,
                endIndex,
                scopes));

            this._lastTokenEndIndex = endIndex;
        }


        internal IToken[] GetResult(StateStack stack, int lineLength)
        {
            if (this._tokens.Count != 0 && this._tokens[this._tokens.Count - 1].StartIndex == lineLength - 1)
            {
                // pop produced token for newline
                this._tokens.RemoveAt(this._tokens.Count - 1);
            }

            if (this._tokens.Count == 0)
            {
                this._lastTokenEndIndex = -1;
                this.Produce(stack, lineLength);
                this._tokens[this._tokens.Count - 1].StartIndex = 0;
            }

            return this._tokens.ToArray();
        }

        internal int[] GetBinaryResult(StateStack stack, int lineLength)
        {
            if (this.binaryTokens.Count != 0 && this.binaryTokens[this.binaryTokens.Count - 2] == lineLength - 1)
            {
                // pop produced token for newline
                this.binaryTokens.RemoveAt(this.binaryTokens.Count - 1);
                this.binaryTokens.RemoveAt(this.binaryTokens.Count - 1);
            }

            if (this.binaryTokens.Count == 0)
            {
                this._lastTokenEndIndex = -1;
                this.Produce(stack, lineLength);
                this.binaryTokens[this.binaryTokens.Count - 2] = 0;
            }

            return this.binaryTokens.ToArray();
        }
    }
}