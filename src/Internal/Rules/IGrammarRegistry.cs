using TextMateSharp.Internal.Types;

namespace TextMateSharp.Internal.Rules
{
    public interface IGrammarRegistry
    {
        IRawGrammar GetExternalGrammar(string scopeName, IRawRepository repository);
    }
}
