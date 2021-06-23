using System.Collections.Generic;

namespace TextMateSharp.Grammars
{
    public interface IToken
    {
        int StartIndex { get; set; }

        int EndIndex { get; }

        List<string> Scopes { get;}
    }
}
