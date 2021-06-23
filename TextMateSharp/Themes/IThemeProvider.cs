using System.Collections.Generic;

namespace TextMateSharp.Themes
{
    public interface IThemeProvider
    {

        List<ThemeTrieElementRule> ThemeMatch(string scopeName);

        ThemeTrieElementRule GetDefaults();
    }
}
