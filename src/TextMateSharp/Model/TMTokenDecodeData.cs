using System.Collections.Generic;

namespace TextMateSharp.Model
{
    public class TMTokenDecodeData
    {
        public string[] Scopes { get; private set; }
        public Dictionary<int, Dictionary<int, bool>> ScopeTokensMaps { get; private set; }

        public TMTokenDecodeData(string[] scopes, Dictionary<int, Dictionary<int, bool>> scopeTokensMaps)
        {
            Scopes = scopes;
            ScopeTokensMaps = scopeTokensMaps;
        }
    }
}