using System.Collections.Generic;

namespace TextMateSharp.Model
{
    public class TMToken
    {
        public int StartIndex;
        public List<string> Scopes { get; private set; }

        public TMToken(int startIndex, List<string> scopes)
        {
            this.StartIndex = startIndex;
            this.Scopes = scopes;
        }
    }
}