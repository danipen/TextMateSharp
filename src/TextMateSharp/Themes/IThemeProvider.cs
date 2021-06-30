using System.Collections.Generic;

namespace TextMateSharp.Themes
{
    public interface IThemeProvider
    {

        List<ThemeTrieElementRule> ThemeMatch(IEnumerable<string> scopeNames);

        ThemeTrieElementRule GetDefaults();
    }
}
