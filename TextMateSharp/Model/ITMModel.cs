using System.Collections.Generic;

using TextMateSharp.Grammars;

namespace TextMateSharp.Model
{
    public interface ITMModel
    {
        IGrammar GetGrammar();
        void SetGrammar(IGrammar grammar);
        void AddModelTokensChangedListener(IModelTokensChangedListener listener);
        void RemoveModelTokensChangedListener(IModelTokensChangedListener listener);
        void Dispose();
        List<TMToken> GetLineTokens(int line);
        void ForceTokenization(int lineNumber);

    }
}