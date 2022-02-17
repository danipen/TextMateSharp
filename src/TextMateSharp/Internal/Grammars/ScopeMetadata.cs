using System.Collections.Generic;

using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars
{
    public class ScopeMetadata
    {

        public string ScopeName { get; private set; }
        public int LanguageId { get; private set; }
        public int TokenType { get; private set; }
        public List<ThemeTrieElementRule> ThemeData { get; private set; }

        public ScopeMetadata(string scopeName, int languageId, int tokenType, List<ThemeTrieElementRule> themeData)
        {
            ScopeName = scopeName;
            LanguageId = languageId;
            TokenType = tokenType;
            ThemeData = themeData;
        }
    }
}