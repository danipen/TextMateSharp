using System.Collections.Generic;
using System.Text;

namespace TextMateSharp.Model
{
    class DecodeMap
    {
        int lastAssignedId;
        Dictionary<string /* scope */, int[] /* ids */ > scopeToTokenIds;
        Dictionary<string /* token */, int?/* id */ > tokenToTokenId;
        Dictionary<int/* id */, string /* id */ > tokenIdToToken;
        public TMTokenDecodeData prevToken;

        public DecodeMap()
        {
            this.lastAssignedId = 0;
            this.scopeToTokenIds = new Dictionary<string, int[]>();
            this.tokenToTokenId = new Dictionary<string, int?>();
            this.tokenIdToToken = new Dictionary<int, string>();
            this.prevToken = new TMTokenDecodeData(new string[0], new Dictionary<int, Dictionary<int, bool>>());
        }

        public int[] getTokenIds(string scope)
        {
            int[] tokens;
            this.scopeToTokenIds.TryGetValue(scope, out tokens);
            if (tokens != null)
            {
                return tokens;
            }
            string[] tmpTokens = scope.Split("[.]");

            tokens = new int[tmpTokens.Length];
            for (int i = 0; i < tmpTokens.Length; i++)
            {
                string token = tmpTokens[i];
                int? tokenId;
                this.tokenToTokenId.TryGetValue(token, out tokenId);
                if (tokenId == null)
                {
                    tokenId = (++this.lastAssignedId);
                    this.tokenToTokenId[token] = tokenId.Value;
                    this.tokenIdToToken[tokenId.Value] = token;
                }
                tokens[i] = tokenId.Value;
            }

            this.scopeToTokenIds[scope] = tokens;
            return tokens;
        }

        public string GetToken(Dictionary<int, bool> tokenMap)
        {
            StringBuilder result = new StringBuilder();
            bool isFirst = true;
            for (int i = 1; i <= this.lastAssignedId; i++)
            {
                if (tokenMap.ContainsKey(i))
                {
                    if (isFirst)
                    {
                        isFirst = false;
                        result.Append(this.tokenIdToToken[i]);
                    }
                    else
                    {
                        result.Append('.');
                        result.Append(this.tokenIdToToken[i]);
                    }
                }
            }
            return result.ToString();
        }
    }
}