using System.Collections.Generic;
using System.Text;

using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars
{
    class Token : IToken
    {
        private int startIndex;

        private int endIndex;

        private List<string> scopes;

        public Token(int startIndex, int endIndex, List<string> scopes)
        {
            this.startIndex = startIndex;
            this.endIndex = endIndex;
            this.scopes = scopes;
        }

        public int GetStartIndex()
        {
            return startIndex;
        }

        public void SetStartIndex(int startIndex)
        {
            this.startIndex = startIndex;
        }

        public int GetEndIndex()
        {
            return endIndex;
        }

        public List<string> GetScopes()
        {
            return scopes;
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.Append("{startIndex: ");
            s.Append(startIndex);
            s.Append(", endIndex: ");
            s.Append(endIndex);
            s.Append(", scopes: ");
            s.Append(scopes);
            s.Append("}");
            return s.ToString();
        }
    }
}