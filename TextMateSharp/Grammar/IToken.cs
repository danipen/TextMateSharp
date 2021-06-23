using System.Collections.Generic;

namespace TextMateSharp.Grammars
{
    public interface IToken
    {
        int GetStartIndex();

        void SetStartIndex(int startIndex);

        int GetEndIndex();

        List<string> GetScopes();
    }
}
