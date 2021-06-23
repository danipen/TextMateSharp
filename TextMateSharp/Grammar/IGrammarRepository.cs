using System.Collections.Generic;

using TextMateSharp.Internal.Types;

namespace TextMateSharp.Grammars
{
    public interface IGrammarRepository
    {
        IRawGrammar Lookup(string scopeName);
        ICollection<string> Injections(string targetScope);
    }
}