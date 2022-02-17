using System.Collections.Generic;

namespace TextMateSharp.Model
{
    public class TMToken
    {
        public int StartIndex { get; private set; }
        public List<string> Scopes { get; private set; }

        public TMToken(int startIndex, List<string> scopes)
        {
            StartIndex = startIndex;
            Scopes = scopes;
        }
    }
}