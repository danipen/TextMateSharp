using System;
using System.Collections.Generic;
using System.Text;

namespace TextMateSharp.Model
{
    class DecodeMap
    {
        public TMTokenDecodeData PrevToken { get; set; }

        private int lastAssignedId;
        private Dictionary<string /* scope */, int[] /* ids */ > _scopeToTokenIds;
        private Dictionary<string /* token */, int?/* id */ > _tokenToTokenId;
        private Dictionary<int/* id */, string /* id */ > _tokenIdToToken;

        public DecodeMap()
        {
            this.PrevToken = new TMTokenDecodeData(new string[0], new Dictionary<int, Dictionary<int, bool>>());

            this.lastAssignedId = 0;
            this._scopeToTokenIds = new Dictionary<string, int[]>();
            this._tokenToTokenId = new Dictionary<string, int?>();
            this._tokenIdToToken = new Dictionary<int, string>();
        }

        public int[] getTokenIds(string scope)
        {
            int[] tokens;
            this._scopeToTokenIds.TryGetValue(scope, out tokens);
            if (tokens != null)
            {
                return tokens;
            }

            string[] tmpTokens = scope.Split(new string[] { "[.]" }, StringSplitOptions.None);

            tokens = new int[tmpTokens.Length];
            for (int i = 0; i < tmpTokens.Length; i++)
            {
                string token = tmpTokens[i];
                int? tokenId;
                this._tokenToTokenId.TryGetValue(token, out tokenId);
                if (tokenId == null)
                {
                    tokenId = (++this.lastAssignedId);
                    this._tokenToTokenId[token] = tokenId.Value;
                    this._tokenIdToToken[tokenId.Value] = token;
                }
                tokens[i] = tokenId.Value;
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
                if (tokenMap.ContainsKey(i))
                {
                    if (isFirst)
                    {
                        isFirst = false;
                        result.Append(this._tokenIdToToken[i]);
                    }
                    else
                    {
                        result.Append('.');
                        result.Append(this._tokenIdToToken[i]);
                    }
                }
            }
            return result.ToString();
        }
    }
}