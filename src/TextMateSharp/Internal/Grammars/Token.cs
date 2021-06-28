using System.Collections.Generic;
using System.Text;

using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars
{
    class Token : IToken
    {
        public int StartIndex { get; set; }

        public int EndIndex { get; private set; }

        public List<string> Scopes { get; private set; }

        public Token(int startIndex, int endIndex, List<string> scopes)
        {
            this.StartIndex = startIndex;
            this.EndIndex = endIndex;
            this.Scopes = scopes;
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.Append("{startIndex: ");
            s.Append(StartIndex);
            s.Append(", endIndex: ");
            s.Append(EndIndex);
            s.Append(", scopes: ");
            s.Append(string.Join(", ", Scopes));
            s.Append("}");
            return s.ToString();
        }
    }
}