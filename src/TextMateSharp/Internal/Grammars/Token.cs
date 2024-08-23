using System.Collections.Generic;
using System.Text;

using TextMateSharp.Grammars;

namespace TextMateSharp.Internal.Grammars
{
    class Token : IToken
    {
        public int StartIndex { get; set; }

        public int EndIndex { get; private set; }

        public int Length { get { return EndIndex - StartIndex; } }

        public List<string> Scopes { get; private set; }

        public Token(int startIndex, int endIndex, List<string> scopes)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
            Scopes = scopes;
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
            s.Append('}');
            return s.ToString();
        }
    }
}