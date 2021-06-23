using System.Collections.Generic;

using TextMateSharp.Grammars;

namespace TextMateSharp.Model
{
    public class Tokenizer : ITokenizationSupport
    {
        private IGrammar grammar;
        private DecodeMap decodeMap;

        public Tokenizer(IGrammar grammar)
        {
            this.grammar = grammar;
            this.decodeMap = new DecodeMap();
        }

        public TMState GetInitialState()
        {
            return new TMState(null, null);
        }

        public LineTokens Tokenize(string line, TMState state)
        {
            return Tokenize(line, state, 0, 0);
        }

        public LineTokens Tokenize(string line, TMState state, int offsetDelta, int stopAtOffset)
        {
            // Do not attempt to tokenize if a line has over 20k
            // or if the rule stack contains more than 100 rules (indicator of
            // broken grammar that forgets to pop rules)
            //if (line.length >= 20000 || depth(state.ruleStack) > 100) {
            // return new RawLineTokens(
            // [new Token(offsetDelta, '')],
            // [new ModeTransition(offsetDelta, this._modeId)],
            // offsetDelta,
            // state
            // );
            // }
            TMState freshState = state != null ? state.Clone() : GetInitialState();
            ITokenizeLineResult textMateResult = grammar.TokenizeLine(line, freshState.GetRuleStack());
            freshState.setRuleStack(textMateResult.GetRuleStack());

            // Create the result early and fill in the tokens later
            List<TMToken> tokens = new List<TMToken>();
            string lastTokenType = null;
            IToken[] tmResultTokens = textMateResult.GetTokens();
            for (int tokenIndex = 0, len = tmResultTokens.Length; tokenIndex < len; tokenIndex++)
            {
                IToken token = tmResultTokens[tokenIndex];
                int tokenStartIndex = token.GetStartIndex();
                string tokenType = DecodeTextMateToken(this.decodeMap, token.GetScopes());

                // do not push a new token if the type is exactly the same (also
                // helps with ligatures)
                if (!tokenType.Equals(lastTokenType))
                {
                    tokens.Add(new TMToken(tokenStartIndex + offsetDelta, tokenType));
                    lastTokenType = tokenType;
                }
            }
            return new LineTokens(tokens, offsetDelta + line.Length, freshState);

        }

        private string DecodeTextMateToken(DecodeMap decodeMap, List<string> scopes)
        {
            string[] prevTokenScopes = decodeMap.prevToken.scopes;
            int prevTokenScopesLength = prevTokenScopes.Length;
            Dictionary<int, Dictionary<int, bool>> prevTokenScopeTokensMaps = decodeMap.prevToken.scopeTokensMaps;

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

            decodeMap.prevToken = new TMTokenDecodeData(scopes.ToArray(), scopeTokensMaps);
            return decodeMap.GetToken(prevScopeTokensMaps);
        }
    }
}