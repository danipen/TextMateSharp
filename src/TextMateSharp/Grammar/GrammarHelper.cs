using System.Collections.Generic;

using TextMateSharp.Internal.Grammars;
using TextMateSharp.Internal.Oniguruma;
using TextMateSharp.Internal.Types;
using TextMateSharp.Themes;

namespace TextMateSharp.Grammars
{
    public static class GrammarHelper
    {
        public static IGrammar CreateGrammar(
            string scopeName,
            IRawGrammar grammar,
            int initialLanguage,
            Dictionary<string, int> embeddedLanguages,
            IGrammarRepository repository,
            IThemeProvider themeProvider)
        {
            return new Grammar(
                scopeName,
                grammar,
                initialLanguage,
                embeddedLanguages,
                /*TODO*/null,
                /*TODO*/null,
                repository,
                themeProvider);
        }
    }
}