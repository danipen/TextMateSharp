using System;
using System.Collections.Generic;

namespace TextMateSharp.Grammars
{
    public interface IGrammar
    {
        bool IsCompiling { get; }
        string GetName();
        string GetScopeName();
        ICollection<string> GetFileTypes();
        ITokenizeLineResult TokenizeLine(LineText lineText);
        ITokenizeLineResult TokenizeLine(LineText lineText, IStateStack prevState, TimeSpan timeLimit);
        ITokenizeLineResult2 TokenizeLine2(LineText lineText);
        ITokenizeLineResult2 TokenizeLine2(LineText lineText, IStateStack prevState, TimeSpan timeLimit);
    }
}