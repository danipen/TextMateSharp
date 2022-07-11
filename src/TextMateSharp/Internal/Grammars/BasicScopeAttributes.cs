using System.Collections.Generic;

using TextMateSharp.Themes;

namespace TextMateSharp.Internal.Grammars
{
    public class BasicScopeAttributes
    {
        public string ScopeName { get; private set; }
        public int LanguageId { get; private set; }
        public int TokenType { get; private set; } /* OptionalStandardTokenType */
        public List<ThemeTrieElementRule> ThemeData { get; private set; }

        public BasicScopeAttributes(
            string scopeName,
            int languageId,
            int tokenType,
            List<ThemeTrieElementRule> themeData)
        {
            ScopeName = scopeName;
            LanguageId = languageId;
            TokenType = tokenType;
            ThemeData = themeData;
        }
    }
}