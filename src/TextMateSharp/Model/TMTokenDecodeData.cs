using System.Collections.Generic;

namespace TextMateSharp.Model
{
    public class TMTokenDecodeData
    {
        public string[] scopes;
        public Dictionary<int, Dictionary<int, bool>> scopeTokensMaps;

        public TMTokenDecodeData(string[] scopes, Dictionary<int, Dictionary<int, bool>> scopeTokensMaps)
        {
            this.scopes = scopes;
            this.scopeTokensMaps = scopeTokensMaps;
        }
    }
}