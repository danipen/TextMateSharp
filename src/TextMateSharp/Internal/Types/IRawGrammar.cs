using System.Collections.Generic;

namespace TextMateSharp.Internal.Types
{
    public interface IRawGrammar
    {
        IRawGrammar Clone();
        IRawRepository GetRepository();

        string GetScopeName();

        ICollection<IRawRule> GetPatterns();

        Dictionary<string, IRawRule> GetInjections();

        string GetInjectionSelector();

        ICollection<string> GetFileTypes();

        string GetName();

        string GetFirstLineMatch();
    }
}