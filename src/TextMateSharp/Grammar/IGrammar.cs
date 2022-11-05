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
        ITokenizeLineResult TokenizeLine(string lineText);
        ITokenizeLineResult TokenizeLine(string lineText, IStateStack prevState, TimeSpan timeLimit);
        ITokenizeLineResult2 TokenizeLine2(string lineText);
        ITokenizeLineResult2 TokenizeLine2(string lineText, IStateStack prevState, TimeSpan timeLimit);
    }
}