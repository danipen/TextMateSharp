using System.Collections.Generic;

namespace TextMateSharp.Grammars
{
    public interface IGrammar
    {
        string GetName();
        string GetScopeName();
        ICollection<string> GetFileTypes();
        ITokenizeLineResult TokenizeLine(string lineText);
        ITokenizeLineResult TokenizeLine(string lineText, StackElement prevState);
        ITokenizeLineResult2 TokenizeLine2(string lineText);
        ITokenizeLineResult2 TokenizeLine2(string lineText, StackElement prevState);
    }
}