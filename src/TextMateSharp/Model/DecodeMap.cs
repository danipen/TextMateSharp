using System;
using System.Collections.Generic;
using System.Text;

namespace TextMateSharp.Model
{
    class DecodeMap
    {
        public TMTokenDecodeData PrevToken { get; set; }

        private int lastAssignedId;
        private readonly Dictionary<string /* scope */, int[] /* ids */ > _scopeToTokenIds;
        private readonly Dictionary<string /* token */, int/* id */ > _tokenToTokenId;
        private readonly List<string> _tokenIdToToken;
        private const char ScopeSeparator = '.';

        public DecodeMap()
        {
            this.PrevToken = new TMTokenDecodeData(Array.Empty<string>(), new Dictionary<int, Dictionary<int, bool>>());

            this.lastAssignedId = 0;
            this._scopeToTokenIds = new Dictionary<string, int[]>();
            this._tokenToTokenId = new Dictionary<string, int>();

            // Index 0 is unused so tokenId can be used directly as the index
            this._tokenIdToToken = new List<string>
            {
                string.Empty // placeholder for index 0
            };
        }

        public int[] getTokenIds(string scope)
        {
            int[] tokens;
            this._scopeToTokenIds.TryGetValue(scope, out tokens);
            if (tokens != null)
            {
                return tokens;
            }

            ReadOnlySpan<char> scopeSpan = scope.AsSpan();

            int tokenCount = 1;
            for (int i = 0; i < scopeSpan.Length; i++)
            {
                if (scopeSpan[i] == ScopeSeparator)
                {
                    tokenCount++;
                }
            }

            tokens = new int[tokenCount];

            int tokenIndex = 0;
            int start = 0;
            for (int i = 0; i <= scopeSpan.Length; i++)
            {
                if (i == scopeSpan.Length || scopeSpan[i] == ScopeSeparator)
                {
                    int length = i - start;
                    string token = scope.Substring(start, length);

                    if (!this._tokenToTokenId.TryGetValue(token, out int tokenId))
                    {
                        tokenId = ++this.lastAssignedId;
                        this._tokenToTokenId[token] = tokenId;
                        this._tokenIdToToken.Add(token);
                    }

                    tokens[tokenIndex] = tokenId;
                    tokenIndex++;

                    start = i + 1;
                }
            }

            this._scopeToTokenIds[scope] = tokens;
            return tokens;
        }

        public string GetToken(Dictionary<int, bool> tokenMap)
        {
            StringBuilder result = new StringBuilder();
            bool isFirst = true;
            for (int i = 1; i <= this.lastAssignedId; i++)
            {
                if (tokenMap.TryGetValue(i, out bool isPresent) && isPresent)
                {
                    if (!isFirst)
                    {
                        result.Append(ScopeSeparator);
                    }
                    else
                    {
                        isFirst = false;
                    }

                    result.Append(this._tokenIdToToken[i]);
                }
            }
            return result.ToString();
        }
    }
}
