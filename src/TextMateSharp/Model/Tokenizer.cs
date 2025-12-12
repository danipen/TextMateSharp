using System;
using System.Collections.Generic;

using TextMateSharp.Grammars;

namespace TextMateSharp.Model
{
    public class Tokenizer : ITokenizationSupport
    {
        private IGrammar _grammar;
        private DecodeMap _decodeMap;

        public Tokenizer(IGrammar grammar)
        {
            this._grammar = grammar;
            this._decodeMap = new DecodeMap();
        }

        public TMState GetInitialState()
        {
            return new TMState(null, null);
        }

        public LineTokens Tokenize(LineText line, TMState state, TimeSpan timeLimit)
        {
            return Tokenize(line, state, 0, 0, timeLimit);
        }

        public LineTokens Tokenize(LineText line, TMState state, int offsetDelta, int maxLen, TimeSpan timeLimit)
        {
            if (_grammar == null)
                return null;

            TMState freshState = state != null ? state.Clone() : GetInitialState();

            ReadOnlyMemory<char> effectiveLine = line.Memory;
            if (maxLen > 0 && effectiveLine.Length > maxLen)
                effectiveLine = effectiveLine.Slice(0, maxLen);

            ITokenizeLineResult textMateResult = _grammar.TokenizeLine(effectiveLine, freshState.GetRuleStack(), timeLimit);
            freshState.SetRuleStack(textMateResult.RuleStack);

            // Create the result early and fill in the tokens later
            List<TMToken> tokens = new List<TMToken>();
            string lastTokenType = null;
            IToken[] tmResultTokens = textMateResult.Tokens;
            for (int tokenIndex = 0, len = tmResultTokens.Length; tokenIndex < len; tokenIndex++)
            {
                IToken token = tmResultTokens[tokenIndex];
                int tokenStartIndex = token.StartIndex;
                string tokenType = DecodeTextMateToken(this._decodeMap, token.Scopes);

                // do not push a new token if the type is exactly the same (also
                // helps with ligatures)
                if (!tokenType.Equals(lastTokenType))
                {
                    tokens.Add(new TMToken(tokenStartIndex + offsetDelta, token.Scopes));
                    lastTokenType = tokenType;
                }
            }
            return new LineTokens(tokens, offsetDelta + effectiveLine.Length, freshState);
        }

        private string DecodeTextMateToken(DecodeMap decodeMap, List<string> scopes)
        {
            string[] prevTokenScopes = decodeMap.PrevToken.Scopes;
            int prevTokenScopesLength = prevTokenScopes.Length;
            Dictionary<int, Dictionary<int, bool>> prevTokenScopeTokensMaps = decodeMap.PrevToken.ScopeTokensMaps;

            Dictionary<int, Dictionary<int, bool>> scopeTokensMaps = new Dictionary<int, Dictionary<int, bool>>();
            Dictionary<int, bool> prevScopeTokensMaps = new Dictionary<int, bool>();
            bool sameAsPrev = true;
            for (int level = 1/* deliberately skip scope 0 */; level < scopes.Count; level++)
            {
                string scope = scopes[level];

                if (sameAsPrev)
                {
                    if (level < prevTokenScopesLength && prevTokenScopes[level].Equals(scope))
                    {
                        prevScopeTokensMaps = prevTokenScopeTokensMaps[level];
                        scopeTokensMaps[level] = prevScopeTokensMaps;
                        continue;
                    }
                    sameAsPrev = false;
                }

                int[] tokens = decodeMap.getTokenIds(scope);
                prevScopeTokensMaps = new Dictionary<int, bool>(prevScopeTokensMaps);
                foreach (int token in tokens)
                {
                    prevScopeTokensMaps[token] = true;
                }
                scopeTokensMaps[level] = prevScopeTokensMaps;
            }

            decodeMap.PrevToken = new TMTokenDecodeData(scopes.ToArray(), scopeTokensMaps);
            return decodeMap.GetToken(prevScopeTokensMaps);
        }
    }
}